// =============================================================================
// Services/SegmentedDownloadEngine.cs
// Download engine: multi-thread segmented (chunk-based) with IProgress<T>
// UTF-8 with BOM – .NET Framework 4.8 / C# 7.3
// =============================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace GMTPC.Tool.Services
{
    /// <summary>
    /// Progress info reported on every UI tick.
    /// </summary>
    public sealed class DownloadProgressInfo
    {
        /// <summary>Bytes downloaded so far across all segments.</summary>
        public long BytesDone { get; set; }

        /// <summary>Total file size in bytes (0 if unknown).</summary>
        public long TotalBytes { get; set; }

        /// <summary>EMA-smoothed download speed in bytes/sec.</summary>
        public double SpeedBytesPerSec { get; set; }

        /// <summary>
        /// Percentage (0-100) for each segment/region.
        /// Length == number of active segments.
        /// </summary>
        public int[] SegmentPercents { get; set; }

        /// <summary>Overall percentage (0-100). 0 if TotalBytes unknown.</summary>
        public int OverallPercent =>
            TotalBytes > 0 ? (int)Math.Min(100, BytesDone * 100L / TotalBytes) : 0;
    }

    /// <summary>
    /// Stateless, thread-safe download engine.
    /// Call <see cref="DownloadAsync"/> once per file; it is NOT reentrant for the same instance
    /// but multiple independent instances may run simultaneously.
    /// </summary>
    public sealed class SegmentedDownloadEngine
    {
        // ── tunables ──────────────────────────────────────────────────────────
        private const long ChunkSize = 512L * 1024;         // 512 KB per work-item (reduced from 8MB for faster startup)
        private const long MinSizeForSegmented = 512L * 1024; // 512 KB minimum (aligned with chunk size)
        private const int  MaxRedirects = 10;
        private const int  MaxRetries   = 10;
        private static readonly TimeSpan HeadTimeout   = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan ChunkTimeout  = TimeSpan.FromMinutes(60);
        private const double EmaAlpha = 0.25;

        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Downloads <paramref name="url"/> to <paramref name="destinationPath"/> using up to
        /// <paramref name="segments"/> parallel streams.
        /// Progress is reported ~5 times/sec via <paramref name="progress"/> (may be null).
        /// </summary>
        public async Task DownloadAsync(
            string url,
            string destinationPath,
            int segments,
            IProgress<DownloadProgressInfo> progress,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(url))   throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));
            segments = Math.Max(1, segments);

            // --- Step 1: probe for file size & range support ----------------
            var probe = await ProbeAsync(url, ct);
            string finalUrl     = probe.FinalUrl;
            long   fileSize     = probe.FileSize;
            bool   supportsRange = probe.SupportsRange;

            // --- Step 2: choose strategy ------------------------------------
            if (!supportsRange || fileSize < MinSizeForSegmented)
            {
                await DownloadSingleAsync(finalUrl, destinationPath, progress, ct);
                return;
            }

            // --- Step 3: segmented download ---------------------------------
            await DownloadSegmentedAsync(finalUrl, destinationPath, fileSize, segments, progress, ct);
        }

        // ── ProbeResult ───────────────────────────────────────────────────────
        private struct ProbeResult
        {
            public string FinalUrl;
            public long   FileSize;
            public bool   SupportsRange;
        }

        // ── Probe ────────────────────────────────────────────────────────────

        private async Task<ProbeResult> ProbeAsync(string url, CancellationToken ct)
        {
            var result = new ProbeResult { FinalUrl = url, FileSize = 0, SupportsRange = false };

            // Try HEAD first (follows redirects)
            try
            {
                using (var h = new HttpClientHandler { AllowAutoRedirect = true })
                using (var c = new HttpClient(h) { Timeout = HeadTimeout })
                {
                    c.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    using (var cts2 = CancellationTokenSource.CreateLinkedTokenSource(
                        ct, new CancellationTokenSource(HeadTimeout).Token))
                    {
                        var resp = await c.SendAsync(
                            new HttpRequestMessage(HttpMethod.Head, url),
                            HttpCompletionOption.ResponseHeadersRead, cts2.Token);

                        result.FinalUrl      = resp.RequestMessage?.RequestUri?.AbsoluteUri ?? url;
                        result.FileSize      = resp.Content.Headers.ContentLength ?? 0;
                        result.SupportsRange = resp.Headers.AcceptRanges.Contains("bytes");
                    }
                }
            }
            catch { /* HEAD not supported – fall through */ }

            if (result.FileSize > 0 && result.SupportsRange) return result;

            // Probe with GET Range:0-0 following redirects manually
            try
            {
                string probeUrl = result.FinalUrl;
                using (var h = new HttpClientHandler { AllowAutoRedirect = false })
                using (var c = new HttpClient(h) { Timeout = HeadTimeout })
                {
                    c.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    using (var cts2 = CancellationTokenSource.CreateLinkedTokenSource(
                        ct, new CancellationTokenSource(HeadTimeout).Token))
                    {
                        for (int rd = 0; rd < MaxRedirects; rd++)
                        {
                            var req = new HttpRequestMessage(HttpMethod.Get, probeUrl);
                            req.Headers.Range = new RangeHeaderValue(0, 0);
                            var resp = await c.SendAsync(req,
                                HttpCompletionOption.ResponseHeadersRead, cts2.Token);

                            int status = (int)resp.StatusCode;
                            if (status >= 300 && status <= 399 && resp.Headers.Location != null)
                            {
                                probeUrl = resp.Headers.Location.IsAbsoluteUri
                                    ? resp.Headers.Location.AbsoluteUri
                                    : new Uri(new Uri(probeUrl), resp.Headers.Location).AbsoluteUri;
                                resp.Dispose();
                                continue;
                            }

                            if (resp.StatusCode == System.Net.HttpStatusCode.PartialContent)
                            {
                                result.SupportsRange = true;
                                result.FileSize      = resp.Content.Headers.ContentRange?.Length ?? 0;
                                result.FinalUrl      = probeUrl;
                            }
                            else if (resp.IsSuccessStatusCode)
                            {
                                result.FileSize  = resp.Content.Headers.ContentLength ?? 0;
                                result.FinalUrl  = probeUrl;
                            }
                            resp.Dispose();
                            break;
                        }
                    }
                }
            }
            catch { /* best-effort */ }

            return result;
        }

        // ── Single-thread fallback ────────────────────────────────────────────

        private async Task DownloadSingleAsync(
            string url,
            string destinationPath,
            IProgress<DownloadProgressInfo> progress,
            CancellationToken ct)
        {
            int retry = 0;
            while (true)
            {
                try
                {
                    using (var c = new HttpClient { Timeout = ChunkTimeout })
                    {
                        c.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                        using (var resp = await c.GetAsync(url,
                            HttpCompletionOption.ResponseHeadersRead, ct))
                        {
                            resp.EnsureSuccessStatusCode();
                            long total    = resp.Content.Headers.ContentLength ?? 0;
                            long done     = 0;
                            double speed  = 0;
                            long lastBytes = 0;
                            var lastTime   = DateTime.Now;

                            if (total > 0)
                            {
                                using (var fs = new FileStream(destinationPath,
                                    FileMode.Create, FileAccess.Write, FileShare.None))
                                    fs.SetLength(total);
                            }

                            using (var stream = await resp.Content.ReadAsStreamAsync())
                            using (var fs = new FileStream(destinationPath,
                                total > 0 ? FileMode.Open : FileMode.Create,
                                FileAccess.Write, FileShare.None, 81920, true))
                            {
                                byte[] buf = new byte[81920];
                                int read;
                                var lastReport = DateTime.Now;

                                while ((read = await stream.ReadAsync(buf, 0, buf.Length, ct)) > 0)
                                {
                                    await fs.WriteAsync(buf, 0, read, ct);
                                    done += read;

                                    var now = DateTime.Now;
                                    double elapsed = (now - lastTime).TotalSeconds;
                                    if (elapsed >= 0.25)
                                    {
                                        double raw = (done - lastBytes) / elapsed;
                                        speed = speed == 0 ? raw : EmaAlpha * raw + (1 - EmaAlpha) * speed;
                                        lastBytes = done;
                                        lastTime  = now;
                                    }

                                    if ((now - lastReport).TotalMilliseconds >= 200)
                                    {
                                        lastReport = now;
                                        progress?.Report(new DownloadProgressInfo
                                        {
                                            BytesDone        = done,
                                            TotalBytes       = total,
                                            SpeedBytesPerSec = speed,
                                            SegmentPercents  = new[] { total > 0 ? (int)(done * 100 / total) : 0 }
                                        });
                                    }
                                }
                            }
                        }
                    }
                    return; // success
                }
                catch (OperationCanceledException) { throw; }
                catch
                {
                    if (++retry >= MaxRetries) throw;
                    if (File.Exists(destinationPath))
                        try { File.Delete(destinationPath); } catch { }
                    await Task.Delay(1000 * (int)Math.Pow(2, retry), ct);
                }
            }
        }

        // ── Segmented download ────────────────────────────────────────────────

        private sealed class ChunkRange
        {
            public long Start;
            public long End;
            public long Downloaded;
            public long Length => End - Start + 1;
        }

        private async Task DownloadSegmentedAsync(
            string url,
            string destinationPath,
            long fileSize,
            int segments,
            IProgress<DownloadProgressInfo> progress,
            CancellationToken ct)
        {
            // Pre-allocate placeholder
            using (var ph = new FileStream(destinationPath,
                FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                ph.SetLength(fileSize);

            // Build initial chunk queue (interleaved across regions for visual balance)
            var chunkQueue     = new ConcurrentQueue<ChunkRange>();
            var remainingChunks = new List<ChunkRange>(); // retries
            var remainingLock  = new object();

            long regionSize = fileSize / segments;
            var regionStarts = new long[segments];
            var regionEnds   = new long[segments];
            for (int i = 0; i < segments; i++)
            {
                regionStarts[i] = i * regionSize;
                regionEnds[i]   = (i == segments - 1) ? fileSize - 1 : (i + 1) * regionSize - 1;
            }

            // Interleave chunks across regions
            bool more = true;
            while (more)
            {
                more = false;
                for (int i = 0; i < segments; i++)
                {
                    if (regionStarts[i] <= regionEnds[i])
                    {
                        long end = Math.Min(regionStarts[i] + ChunkSize - 1, regionEnds[i]);
                        chunkQueue.Enqueue(new ChunkRange { Start = regionStarts[i], End = end });
                        regionStarts[i] = end + 1;
                        more = true;
                    }
                }
            }

            // Per-region downloaded bytes for visual progress
            long[] regionDone  = new long[segments];
            long   totalDone   = 0;
            var    lockObj     = new object();

            // Speed tracking
            double smoothedSpeed = 0;
            long   lastSpeedBytes = 0;
            var    lastSpeedTime  = DateTime.Now;

            // UI reporting task
            using (var uiCts = new CancellationTokenSource())
            {
                var uiTask = Task.Run(async () =>
                {
                    while (!uiCts.Token.IsCancellationRequested)
                    {
                        try { await Task.Delay(200, uiCts.Token); } catch { break; }

                        long done;
                        int[] segs;
                        double spd;
                        lock (lockObj)
                        {
                            done = totalDone;
                            segs = new int[segments];
                            for (int i = 0; i < segments; i++)
                            {
                                long rLen = regionEnds[i] - (i * fileSize / segments) + 1;
                                // Recalculate based on original region size
                                long rStart = i * (fileSize / segments);
                                long rEnd   = (i == segments - 1) ? fileSize - 1 : (i + 1) * (fileSize / segments) - 1;
                                long rSize  = rEnd - rStart + 1;
                                segs[i] = rSize > 0 ? (int)Math.Min(100, regionDone[i] * 100 / rSize) : 100;
                            }
                            spd = smoothedSpeed;
                        }

                        progress?.Report(new DownloadProgressInfo
                        {
                            BytesDone        = done,
                            TotalBytes       = fileSize,
                            SpeedBytesPerSec = spd,
                            SegmentPercents  = segs
                        });
                    }
                });

                try
                {
                    int totalRounds = 0;

                    while (!chunkQueue.IsEmpty || remainingChunks.Count > 0)
                    {
                        ct.ThrowIfCancellationRequested();

                        // Re-enqueue failed chunks
                        lock (remainingLock)
                        {
                            if (remainingChunks.Count > 0)
                            {
                                if (totalRounds == 0)
                                    throw new Exception($"Tất cả {segments} luồng thất bại liên tục. Kiểm tra mạng.");
                                foreach (var r in remainingChunks) chunkQueue.Enqueue(r);
                                remainingChunks.Clear();
                            }
                        }

                        totalRounds = 0;
                        using (var sessionCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                        {
                            var workers = new List<Task>();
                            for (int i = 0; i < segments; i++)
                            {
                                int idx = i;
                                workers.Add(Task.Run(async () =>
                                {
                                    using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
                                    using (var client  = new HttpClient(handler) { Timeout = ChunkTimeout })
                                    using (var fs      = new FileStream(destinationPath,
                                        FileMode.Open, FileAccess.Write,
                                        FileShare.ReadWrite, 262144, true))
                                    {
                                        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                                        while (chunkQueue.TryDequeue(out var chunk))
                                        {
                                            if (sessionCts.Token.IsCancellationRequested)
                                            {
                                                lock (remainingLock) { remainingChunks.Add(chunk); }
                                                break;
                                            }
                                            try
                                            {
                                                string curUrl = url;
                                                HttpResponseMessage response = null;

                                                for (int rd = 0; rd < MaxRedirects; rd++)
                                                {
                                                    var req = new HttpRequestMessage(HttpMethod.Get, curUrl);
                                                    req.Headers.Range = new RangeHeaderValue(chunk.Start, chunk.End);
                                                    response = await client.SendAsync(req,
                                                        HttpCompletionOption.ResponseHeadersRead,
                                                        sessionCts.Token);

                                                    int sc = (int)response.StatusCode;
                                                    if (sc >= 300 && sc <= 399 && response.Headers.Location != null)
                                                    {
                                                        curUrl = response.Headers.Location.IsAbsoluteUri
                                                            ? response.Headers.Location.AbsoluteUri
                                                            : new Uri(new Uri(curUrl), response.Headers.Location).AbsoluteUri;
                                                        response.Dispose();
                                                        response = null;
                                                    }
                                                    else break;
                                                }

                                                using (response)
                                                {
                                                    response.EnsureSuccessStatusCode();
                                                    using (var stream = await response.Content.ReadAsStreamAsync())
                                                    {
                                                        fs.Seek(chunk.Start + chunk.Downloaded, SeekOrigin.Begin);
                                                        byte[] buf = new byte[256 * 1024];
                                                        int read;
                                                        int emptyReads = 0;

                                                        while (emptyReads < 3)
                                                        {
                                                            read = await stream.ReadAsync(buf, 0, buf.Length,
                                                                sessionCts.Token);

                                                            if (read > 0)
                                                            {
                                                                emptyReads = 0;
                                                                await fs.WriteAsync(buf, 0, read, sessionCts.Token);
                                                                chunk.Downloaded += read;

                                                                // Region attribution
                                                                long pos = chunk.Start + chunk.Downloaded - read;
                                                                int  rIdx = (int)Math.Min(
                                                                    pos / (fileSize / segments),
                                                                    segments - 1);

                                                                lock (lockObj)
                                                                {
                                                                    totalDone       += read;
                                                                    regionDone[rIdx] += read;

                                                                    // Speed EMA
                                                                    var now = DateTime.Now;
                                                                    double elapsed = (now - lastSpeedTime).TotalSeconds;
                                                                    if (elapsed >= 0.25)
                                                                    {
                                                                        double raw = (totalDone - lastSpeedBytes) / elapsed;
                                                                        smoothedSpeed = smoothedSpeed == 0
                                                                            ? raw
                                                                            : EmaAlpha * raw + (1 - EmaAlpha) * smoothedSpeed;
                                                                        lastSpeedBytes = totalDone;
                                                                        lastSpeedTime  = now;
                                                                    }
                                                                }
                                                            }
                                                            else
                                                            {
                                                                emptyReads++;
                                                                if (emptyReads >= 3 && chunk.Downloaded < chunk.Length)
                                                                {
                                                                    // Stalled – requeue remainder
                                                                    var rem = new ChunkRange
                                                                    {
                                                                        Start      = chunk.Start + chunk.Downloaded,
                                                                        End        = chunk.End,
                                                                        Downloaded = 0
                                                                    };
                                                                    lock (remainingLock) { remainingChunks.Add(rem); }
                                                                    break;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                Interlocked.Increment(ref totalRounds);
                                            }
                                            catch (OperationCanceledException) { throw; }
                                            catch
                                            {
                                                lock (remainingLock) { remainingChunks.Add(chunk); }
                                                sessionCts.Cancel();
                                                throw;
                                            }
                                        }
                                    }
                                }, sessionCts.Token));
                            }

                            try { await Task.WhenAll(workers); }
                            catch (OperationCanceledException) when (!ct.IsCancellationRequested) { /* pause/re-segment */ }
                        }

                        if (totalDone >= fileSize) break;
                    }
                }
                finally
                {
                    uiCts.Cancel();
                    await uiTask;
                }
            }

            // Final 100% report
            progress?.Report(new DownloadProgressInfo
            {
                BytesDone        = fileSize,
                TotalBytes       = fileSize,
                SpeedBytesPerSec = 0,
                SegmentPercents  = new int[segments] // all 100 implicitly via caller reset
            });
        }
    }
}
