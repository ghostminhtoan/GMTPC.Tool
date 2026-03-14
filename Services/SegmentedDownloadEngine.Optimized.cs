// =============================================================================
// Services/SegmentedDownloadEngine.Optimized.cs
// AI Summary:
// Date: 2026-03-14
// - Fixed progress bar freezing during pause/resume
// - UI reporting task linked to cancellation token instead of manual cancellation
// - Added 500ms delay after workers complete for final progress report
// High-performance multi-thread segmented download engine
// Optimizations: Async I/O, buffer pooling, connection tuning, minimal lock contention
// UTF-8 with BOM – .NET Framework 4.8 / C# 7.3
// =============================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace GMTPC.Tool.Services
{
    /// <summary>
    /// High-performance segmented download engine with optimized I/O
    /// Uses DownloadProgressInfo from SegmentedDownloadEngine.cs
    /// </summary>
    public sealed class SegmentedDownloadEngineOptimized
    {
        // ── Performance Tunables ─────────────────────────────────────────────
        private const long ChunkSize = 2L * 1024 * 1024;         // 2 MB chunks for fewer requests
        private const long MinSizeForSegmented = 5L * 1024 * 1024; // 5 MB minimum for segmented
        private const int MaxRedirects = 10;
        private const int MaxRetries = 10;

        // Timeout settings
        private static readonly TimeSpan HeadTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ChunkTimeout = TimeSpan.FromHours(2);

        // Speed calculation
        private const double EmaAlpha = 0.3;  // More responsive to recent speed changes

        // Buffer sizes - OPTIMIZED for >100 MB/s throughput
        private const int NetworkBufferSize = 2097152;           // 2MB network buffer (reduced I/O ops)
        private const int FileBufferSize = 2097152;              // 2MB file buffer for HDD optimization
        private const int MaxConcurrentConnections = 32;         // Allow more concurrent connections
        
        // User-Agent for better server compatibility
        private static readonly string UserAgent = 
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        // Buffer pool for memory efficiency (reduces GC pressure at high throughput)
        private static readonly ConcurrentQueue<byte[]> _bufferPool = new ConcurrentQueue<byte[]>();

        static SegmentedDownloadEngineOptimized()
        {
            // Global connection optimization for .NET Framework
            ServicePointManager.DefaultConnectionLimit = MaxConcurrentConnections;
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.MaxServicePoints = 100;

            // Pre-allocate buffer pool (64MB total pool for 32 threads @ 2MB each)
            for (int i = 0; i < 32; i++)
            {
                _bufferPool.Enqueue(new byte[NetworkBufferSize]);
            }
        }

        /// <summary>
        /// Downloads file using optimized multi-segment approach
        /// On cancellation, partial file is automatically deleted
        /// Supports pause/resume via ManualResetEventSlim
        /// </summary>
        public async Task DownloadAsync(string url, string destinationPath, int segments,
            IProgress<DownloadProgressInfo> progress, CancellationToken ct,
            System.Threading.ManualResetEventSlim pauseEvent = null)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));
            segments = Math.Max(1, Math.Min(segments, 32)); // Cap at 32 segments

            try
            {
                // Probe for file info
                var probe = await ProbeAsync(url, ct);

                // Choose strategy based on file size and server support
                if (!probe.SupportsRange || probe.FileSize < MinSizeForSegmented)
                {
                    await DownloadSingleOptimizedAsync(probe.FinalUrl, destinationPath, progress, ct, pauseEvent);
                    return;
                }

                await DownloadSegmentedOptimizedAsync(probe.FinalUrl, destinationPath, probe.FileSize, segments, progress, ct, pauseEvent);
            }
            catch (OperationCanceledException)
            {
                // On cancellation, clean up partial download
                CleanupPartialDownload(destinationPath);
                throw; // Re-throw to propagate cancellation
            }
            catch (Exception)
            {
                // On any error, clean up partial download
                CleanupPartialDownload(destinationPath);
                throw;
            }
        }

        /// <summary>
        /// FAST PATH: Direct single-connection download without probe
        /// Use this for known direct URLs (GitHub, Cloudflare, etc.) to skip HEAD request overhead
        /// This is the "Golden Standard" for single-link downloads - used by VPN1111
        /// </summary>
        public async Task DownloadSingleFastAsync(string url, string destinationPath, 
            IProgress<DownloadProgressInfo> progress, CancellationToken ct,
            System.Threading.ManualResetEventSlim pauseEvent = null)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));
            if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));

            try
            {
                // Skip probe entirely - go straight to download
                await DownloadSingleOptimizedAsync(url, destinationPath, progress, ct, pauseEvent);
            }
            catch (OperationCanceledException)
            {
                CleanupPartialDownload(destinationPath);
                throw;
            }
            catch (Exception)
            {
                CleanupPartialDownload(destinationPath);
                throw;
            }
        }

        // ── Probe Result ─────────────────────────────────────────────────────
        private struct ProbeResult
        {
            public string FinalUrl;
            public long FileSize;
            public bool SupportsRange;
        }

        // ── Probe with optimized HttpClient ──────────────────────────────────
        /// <summary>
        /// Probes URL to get final destination (after redirects) and file size
        /// Critical for Archive.org and Mediafire which use HTTP 301/302 redirects
        /// </summary>
        private async Task<ProbeResult> ProbeAsync(string url, CancellationToken ct)
        {
            var result = new ProbeResult { FinalUrl = url, FileSize = 0, SupportsRange = false };

            // STEP 1: HEAD request with auto-redirect to get final URL
            try
            {
                using (var handler = new HttpClientHandler
                {
                    AllowAutoRedirect = true,  // Follow redirects automatically
                    UseCookies = false,
                    AutomaticDecompression = DecompressionMethods.None
                })
                using (var client = new HttpClient(handler) { Timeout = HeadTimeout })
                {
                    // Spoof User-Agent to avoid server throttling (Archive.org, Mediafire)
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    client.DefaultRequestHeaders.Add("Accept", "*/*");

                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                    {
                        cts.CancelAfter(HeadTimeout);

                        var request = new HttpRequestMessage(HttpMethod.Head, url);
                        var response = await client.SendAsync(request,
                            HttpCompletionOption.ResponseHeadersRead, cts.Token);

                        // Get the FINAL URL after all redirects
                        result.FinalUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? url;
                        result.FileSize = response.Content.Headers.ContentLength ?? 0;
                        result.SupportsRange = response.Headers.AcceptRanges.Contains("bytes");
                    }
                }
            }
            catch { /* HEAD failed - try GET probe */ }

            if (result.FileSize > 0 && result.SupportsRange) return result;

            // STEP 2: Fallback - GET with Range header and manual redirect following
            try
            {
                string probeUrl = result.FinalUrl;

                using (var handler = new HttpClientHandler 
                { 
                    AllowAutoRedirect = false,  // Manual redirect handling
                    UseCookies = false,
                    AutomaticDecompression = DecompressionMethods.None
                })
                using (var client = new HttpClient(handler) { Timeout = HeadTimeout })
                {
                    // Spoof User-Agent and Accept headers
                    client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                    client.DefaultRequestHeaders.Add("Accept", "*/*");

                    for (int redirect = 0; redirect < MaxRedirects; redirect++)
                    {
                        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                        {
                            cts.CancelAfter(HeadTimeout);

                            var request = new HttpRequestMessage(HttpMethod.Get, probeUrl);
                            request.Headers.Range = new RangeHeaderValue(0, 0);

                            var response = await client.SendAsync(request,
                                HttpCompletionOption.ResponseHeadersRead, cts.Token);

                            int status = (int)response.StatusCode;
                            if (status >= 300 && status <= 399 && response.Headers.Location != null)
                            {
                                // Follow redirect to final destination
                                probeUrl = response.Headers.Location.IsAbsoluteUri
                                    ? response.Headers.Location.AbsoluteUri
                                    : new Uri(new Uri(probeUrl), response.Headers.Location).AbsoluteUri;
                                response.Dispose();
                                continue;
                            }

                            if (response.StatusCode == HttpStatusCode.PartialContent)
                            {
                                result.SupportsRange = true;
                                result.FileSize = response.Content.Headers.ContentRange?.Length ?? 0;
                                result.FinalUrl = probeUrl;
                            }
                            else if (response.IsSuccessStatusCode)
                            {
                                result.FileSize = response.Content.Headers.ContentLength ?? 0;
                                result.FinalUrl = probeUrl;
                            }
                            
                            response.Dispose();
                            break;
                        }
                    }
                }
            }
            catch { /* Best effort */ }

            return result;
        }

        // ── Single Connection Optimized Download ─────────────────────────────
        private async Task DownloadSingleOptimizedAsync(string url, string destinationPath,
            IProgress<DownloadProgressInfo> progress, CancellationToken ct,
            System.Threading.ManualResetEventSlim pauseEvent)
        {
            int retry = 0;

            while (true)
            {
                try
                {
                    using (var handler = new HttpClientHandler
                    {
                        UseCookies = false,
                        AutomaticDecompression = DecompressionMethods.None
                    })
                    using (var client = new HttpClient(handler) { Timeout = ChunkTimeout })
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                        {
                            cts.CancelAfter(ChunkTimeout);

                            var response = await client.GetAsync(url,
                                HttpCompletionOption.ResponseHeadersRead, cts.Token);
                            
                            response.EnsureSuccessStatusCode();
                            
                            long total = response.Content.Headers.ContentLength ?? 0;
                            long done = 0;
                            double speed = 0;
                            long lastBytes = 0;
                            var lastTime = DateTime.Now;

                            // Pre-allocate file to prevent fragmentation
                            if (total > 0)
                            {
                                using (var fs = new FileStream(destinationPath, FileMode.Create,
                                    FileAccess.Write, FileShare.None, FileBufferSize,
                                    FileOptions.Asynchronous))
                                {
                                    fs.SetLength(total);
                                }
                            }

                            using (var stream = await response.Content.ReadAsStreamAsync())
                            using (var fs = new FileStream(destinationPath,
                                total > 0 ? FileMode.Open : FileMode.Create,
                                FileAccess.Write, FileShare.None, FileBufferSize,
                                FileOptions.Asynchronous))
                            {
                                // Get buffer from pool (1MB for maximum throughput)
                                byte[] buffer = GetBuffer();
                                
                                try
                                {
                                    int read;
                                    var lastReport = DateTime.Now;

                                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                                    {
                                        // WAIT ON PAUSE EVENT - non-blocking async wait during pause, continues when resumed
                                        if (pauseEvent != null && !pauseEvent.IsSet)
                                        {
                                            await Task.Run(() => pauseEvent.Wait(cts.Token), cts.Token);
                                        }

                                        // Async write to pre-allocated file
                                        await fs.WriteAsync(buffer, 0, read, cts.Token);
                                        done += read;

                                        // Speed calculation with EMA smoothing
                                        var now = DateTime.Now;
                                        double elapsed = (now - lastTime).TotalSeconds;
                                        if (elapsed >= 0.2)
                                        {
                                            double rawSpeed = (done - lastBytes) / elapsed;
                                            speed = speed == 0 ? rawSpeed : EmaAlpha * rawSpeed + (1 - EmaAlpha) * speed;
                                            lastBytes = done;
                                            lastTime = now;
                                        }

                                        // Progress reporting throttled to 5Hz
                                        if ((now - lastReport).TotalMilliseconds >= 200)
                                        {
                                            lastReport = now;
                                            progress?.Report(new DownloadProgressInfo
                                            {
                                                BytesDone = done,
                                                TotalBytes = total,
                                                SpeedBytesPerSec = speed,
                                                SegmentPercents = new[] { total > 0 ? (int)(done * 100 / total) : 0 }
                                            });
                                        }
                                    }
                                }
                                finally
                                {
                                    ReturnBuffer(buffer);
                                }
                            }
                        }
                    }
                    return; // Success
                }
                catch (OperationCanceledException) { throw; }
                catch
                {
                    if (++retry >= MaxRetries) throw;
                    if (File.Exists(destinationPath))
                        try { File.Delete(destinationPath); } catch { }
                    await Task.Delay(1000 * retry, ct);
                }
            }
        }

        // ── Optimized Segmented Download ─────────────────────────────────────
        /// <summary>
        /// Downloads file using parallel segments with simultaneous connection firing
        /// All 16 segments start at the same time via Task.WhenAll (no sequential ramp-up)
        /// Supports pause/resume via ManualResetEventSlim
        /// </summary>
        private async Task DownloadSegmentedOptimizedAsync(string url, string destinationPath,
            long fileSize, int segments, IProgress<DownloadProgressInfo> progress, CancellationToken ct,
            System.Threading.ManualResetEventSlim pauseEvent)
        {
            // CRITICAL: Pre-allocate file to prevent fragmentation
            // This is essential for HDD performance - avoids scattered writes
            using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write,
                FileShare.ReadWrite, FileBufferSize, FileOptions.Asynchronous))
            {
                fs.SetLength(fileSize);  // Pre-allocate entire file upfront
            }

            // Build chunk queue with interleaved regions for balanced visual progress
            var chunkQueue = new ConcurrentQueue<ChunkRange>();
            var retryQueue = new ConcurrentQueue<ChunkRange>();
            
            long regionSize = fileSize / segments;
            var regionProgress = new long[segments];
            var regionStarts = new long[segments];
            var regionEnds = new long[segments];

            for (int i = 0; i < segments; i++)
            {
                regionStarts[i] = i * regionSize;
                regionEnds[i] = (i == segments - 1) ? fileSize - 1 : (i + 1) * regionSize - 1;
            }

            // Interleave chunks across regions
            bool hasMore = true;
            while (hasMore)
            {
                hasMore = false;
                for (int i = 0; i < segments; i++)
                {
                    if (regionStarts[i] <= regionEnds[i])
                    {
                        long end = Math.Min(regionStarts[i] + ChunkSize - 1, regionEnds[i]);
                        chunkQueue.Enqueue(new ChunkRange
                        {
                            Start = regionStarts[i],
                            End = end,
                            Downloaded = 0
                        });
                        regionStarts[i] = end + 1;
                        hasMore = true;
                    }
                }
            }

            // Shared state for progress tracking - use class for reference semantics
            var progressState = new ProgressState
            {
                TotalDownloaded = 0,
                SmoothedSpeed = 0,
                LastSpeedBytes = 0,
                LastSpeedTime = DateTime.Now,
                Lock = new object()
            };

            // UI reporting task (throttled to 5Hz)
            using (var uiCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                var uiTask = Task.Run(async () =>
                {
                    while (!uiCts.Token.IsCancellationRequested)
                    {
                        try { await Task.Delay(200, uiCts.Token); } catch { break; }

                        long done;
                        double spd;
                        int[] percents;

                        lock (progressState.Lock)
                        {
                            done = progressState.TotalDownloaded;
                            spd = progressState.SmoothedSpeed;
                            percents = new int[segments];
                            for (int i = 0; i < segments; i++)
                            {
                                long rSize = regionEnds[i] - (i * regionSize) + 1;
                                percents[i] = rSize > 0 ? (int)Math.Min(100, regionProgress[i] * 100 / rSize) : 100;
                            }
                        }

                        progress?.Report(new DownloadProgressInfo
                        {
                            BytesDone = done,
                            TotalBytes = fileSize,
                            SpeedBytesPerSec = spd,
                            SegmentPercents = percents
                        });
                    }
                }, uiCts.Token);

                try
                {
                    // CRITICAL: Fire ALL 16 segments SIMULTANEOUSLY with Task.WhenAll
                    // No sequential ramp-up - all HTTP GET requests start at once
                    var workers = new Task[segments];
                    for (int i = 0; i < segments; i++)
                    {
                        int workerIndex = i;
                        workers[i] = Task.Run(async () =>
                        {
                            await ProcessChunksAsync(url, destinationPath, chunkQueue, retryQueue,
                                workerIndex, regionProgress, progressState, ct, pauseEvent);
                        }, ct);
                    }

                    // Wait for ALL workers to complete (or cancel) simultaneously
                    await Task.WhenAll(workers);
                    
                    // After all workers complete, wait a moment for final progress report
                    try { await Task.Delay(500, ct); } catch { }
                }
                finally
                {
                    // Cancel UI task - it will stop when CT is cancelled
                    // UI task is linked to CT, so it will automatically stop on cancellation
                    uiCts.Cancel();
                    try { await uiTask.ConfigureAwait(false); } catch { }
                }
            }
        }

        // ── Progress State Class (reference type) ────────────────────────────
        private sealed class ProgressState
        {
            public long TotalDownloaded;
            public double SmoothedSpeed;
            public long LastSpeedBytes;
            public DateTime LastSpeedTime;
            public object Lock;
        }

        // ── Chunk Processing Worker ──────────────────────────────────────────
        /// <summary>
        /// Each worker opens its own FileStream with RandomAccess for lock-free parallel writes
        /// Each segment writes directly to its pre-allocated offset without global file lock
        /// Waits on pauseEvent during pause - resumes when event is set
        /// </summary>
        private async Task ProcessChunksAsync(string url, string destinationPath,
            ConcurrentQueue<ChunkRange> chunkQueue, ConcurrentQueue<ChunkRange> retryQueue,
            int regionIndex, long[] regionProgress, ProgressState progressState, CancellationToken ct,
            System.Threading.ManualResetEventSlim pauseEvent)
        {
            using (var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                AutomaticDecompression = DecompressionMethods.None
            })
            using (var client = new HttpClient(handler) { Timeout = ChunkTimeout })
            {
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);

                // CRITICAL: Open FileStream with RandomAccess for parallel writes
                // Each worker has its own handle - NO lock contention on disk I/O
                // FileOptions.RandomAccess enables efficient seeking to random positions
                using (var fs = new FileStream(destinationPath, FileMode.Open, FileAccess.Write,
                    FileShare.ReadWrite, FileBufferSize, FileOptions.Asynchronous | FileOptions.RandomAccess))
                {
                    byte[] buffer = GetBuffer();

                    try
                    {
                        while (!ct.IsCancellationRequested)
                        {
                            // WAIT ON PAUSE EVENT - non-blocking async wait during pause, continues when resumed
                            if (pauseEvent != null && !pauseEvent.IsSet)
                            {
                                await Task.Run(() => pauseEvent.Wait(ct), ct);
                            }

                            ChunkRange chunk;

                            // Try primary queue first, then retry queue
                            if (!chunkQueue.TryDequeue(out chunk))
                            {
                                if (!retryQueue.TryDequeue(out chunk))
                                    break; // No more work
                            }

                            try
                            {
                                string currentUrl = url;
                                
                                // Handle redirects
                                for (int redirect = 0; redirect < MaxRedirects; redirect++)
                                {
                                    using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                                    {
                                        cts.CancelAfter(ChunkTimeout);

                                        var request = new HttpRequestMessage(HttpMethod.Get, currentUrl);
                                        request.Headers.Range = new RangeHeaderValue(
                                            chunk.Start + chunk.Downloaded, chunk.End);

                                        var response = await client.SendAsync(request,
                                            HttpCompletionOption.ResponseHeadersRead, cts.Token);

                                        int status = (int)response.StatusCode;
                                        if (status >= 300 && status <= 399 && response.Headers.Location != null)
                                        {
                                            currentUrl = response.Headers.Location.IsAbsoluteUri
                                                ? response.Headers.Location.AbsoluteUri
                                                : new Uri(new Uri(currentUrl), response.Headers.Location).AbsoluteUri;
                                            response.Dispose();
                                            continue;
                                        }

                                        using (response)
                                        {
                                            response.EnsureSuccessStatusCode();

                                            using (var stream = await response.Content.ReadAsStreamAsync())
                                            {
                                                // Seek to exact position - each thread writes to its own region
                                                // No lock needed because each chunk has unique offset
                                                fs.Seek(chunk.Start + chunk.Downloaded, SeekOrigin.Begin);

                                                int read;
                                                int emptyReads = 0;

                                                while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                                                {
                                                    // Direct async write to pre-allocated file
                                                    // RandomAccess allows efficient non-sequential writes
                                                    await fs.WriteAsync(buffer, 0, read, cts.Token);
                                                    chunk.Downloaded += read;

                                                    // Update progress with minimal lock contention
                                                    lock (progressState.Lock)
                                                    {
                                                        progressState.TotalDownloaded += read;
                                                        regionProgress[regionIndex] += read;

                                                        // Speed calculation
                                                        var now = DateTime.Now;
                                                        double elapsed = (now - progressState.LastSpeedTime).TotalSeconds;
                                                        if (elapsed >= 0.2)
                                                        {
                                                            double rawSpeed = (progressState.TotalDownloaded - progressState.LastSpeedBytes) / elapsed;
                                                            progressState.SmoothedSpeed = progressState.SmoothedSpeed == 0
                                                                ? rawSpeed
                                                                : EmaAlpha * rawSpeed + (1 - EmaAlpha) * progressState.SmoothedSpeed;
                                                            progressState.LastSpeedBytes = progressState.TotalDownloaded;
                                                            progressState.LastSpeedTime = now;
                                                        }
                                                    }

                                                    emptyReads = 0;
                                                }

                                                // Handle stalled connections
                                                if (emptyReads >= 2 && chunk.Downloaded < chunk.Length)
                                                {
                                                    retryQueue.Enqueue(new ChunkRange
                                                    {
                                                        Start = chunk.Start + chunk.Downloaded,
                                                        End = chunk.End,
                                                        Downloaded = 0
                                                    });
                                                }
                                            }
                                        }
                                        break;
                                    }
                                }
                            }
                            catch (OperationCanceledException) when (ct.IsCancellationRequested)
                            {
                                throw;
                            }
                            catch
                            {
                                // Re-queue failed chunk
                                retryQueue.Enqueue(new ChunkRange
                                {
                                    Start = chunk.Start + chunk.Downloaded,
                                    End = chunk.End,
                                    Downloaded = 0
                                });
                            }
                        }
                    }
                    finally
                    {
                        ReturnBuffer(buffer);
                    }
                }
            }
        }

        // ── Buffer Pool Management ───────────────────────────────────────────
        private static byte[] GetBuffer()
        {
            if (_bufferPool.TryDequeue(out var buffer))
                return buffer;
            return new byte[NetworkBufferSize];
        }

        private static void ReturnBuffer(byte[] buffer)
        {
            if (buffer?.Length == NetworkBufferSize)
                _bufferPool.Enqueue(buffer);
        }

        // ── Cleanup Helper ───────────────────────────────────────────────────
        /// <summary>
        /// Safely deletes a partial download file after cancellation/failure
        /// Uses fire-and-forget pattern with retry loop to avoid blocking UI
        /// </summary>
        private static void CleanupPartialDownload(string destinationPath)
        {
            // Fire-and-forget: Run deletion in background task
            Task.Run(() =>
            {
                try
                {
                    if (File.Exists(destinationPath))
                    {
                        // Retry loop: Try to delete file multiple times if OS hasn't released handle
                        int maxRetries = 5;
                        int retryDelayMs = 100;
                        
                        for (int attempt = 0; attempt < maxRetries; attempt++)
                        {
                            try
                            {
                                File.Delete(destinationPath);
                                break; // Success - exit retry loop
                            }
                            catch (IOException)
                            {
                                // File still locked by OS - wait and retry
                                if (attempt < maxRetries - 1)
                                {
                                    Thread.Sleep(retryDelayMs);
                                    retryDelayMs *= 2; // Exponential backoff: 100ms, 200ms, 400ms...
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // No permission - give up
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore all cleanup errors - this is fire-and-forget
                }
            });
            // Note: We don't await this task - it runs independently in background
        }

        // ── Chunk Range Helper ───────────────────────────────────────────────
        private sealed class ChunkRange
        {
            public long Start;
            public long End;
            public long Downloaded;
            public long Length => End - Start + 1;
        }
    }
}
