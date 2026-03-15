// =======================================================================
// MainWindow.SystemDownload.cs
// =======================================================================
// CHỨA DUY NHẤT: Logic tải file (Download Engine)
// KHÔNG chứa: Install logic, Process start, MessageBox, UI event handlers
//
// Kiến trúc tải file chuẩn hóa cho toàn bộ ứng dụng
// Tất cả checkbox phải gọi qua các hàm trong file này
//
// Cập nhật: 2026-03-15 - Merged all Services download code (DownloadConfiguration,
//   DownloadRegistry, DownloadTaskManager, SegmentedDownloadEngine, 
//   SegmentedDownloadEngine.Optimized) into this file
// Previous: 2026-03-14 (CRITICAL FIX) - Tăng segments 16→32, fixed file size discovery timeout
// =======================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // ===================================================================
        // DownloadConfiguration - Global download path configuration
        // ===================================================================
        private static class DownloadConfiguration
        {
            private static string _selectedTempPath = null;
            private static readonly object _lock = new object();

            /// <summary>
            /// Gets or sets the global temp base path (e.g., K:\temp)
            /// This is the root folder for ALL downloads
            /// </summary>
            public static string TempBasePath
            {
                get
                {
                    lock (_lock)
                    {
                        return _selectedTempPath;
                    }
                }
                set
                {
                    lock (_lock)
                    {
                        _selectedTempPath = value;
                    }
                }
            }

            /// <summary>
            /// Gets the isolated download folder for a specific task
            /// Format: {TempBasePath}\{TaskName}\
            /// Example: K:\temp\Process Lasso\
            /// </summary>
            public static string GetTaskDownloadFolder(string taskName)
            {
                string basePath;
                lock (_lock)
                {
                    basePath = _selectedTempPath;
                }

                if (string.IsNullOrEmpty(basePath))
                {
                    return Path.Combine(GetGMTPCFolder(), SanitizeFolderName(taskName));
                }

                string taskFolder = Path.Combine(basePath, SanitizeFolderName(taskName));

                if (!Directory.Exists(taskFolder))
                {
                    Directory.CreateDirectory(taskFolder);
                }

                return taskFolder;
            }

            private static string GetGMTPCFolder()
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string gmtpcFolder = Path.Combine(appData, "GMTPC");

                if (!Directory.Exists(gmtpcFolder))
                {
                    Directory.CreateDirectory(gmtpcFolder);
                }

                return gmtpcFolder;
            }

            private static string SanitizeFolderName(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return "Unknown";
                }

                char[] invalidChars = Path.GetInvalidFileNameChars();
                char[] invalidPathChars = Path.GetInvalidPathChars();

                string sanitized = name;
                foreach (char c in invalidChars)
                {
                    sanitized = sanitized.Replace(c, '_');
                }
                foreach (char c in invalidPathChars)
                {
                    sanitized = sanitized.Replace(c, '_');
                }

                sanitized = sanitized.Trim();

                if (string.IsNullOrEmpty(sanitized))
                {
                    return "Download";
                }

                return sanitized;
            }

            public static string SetTempBasePath(string driveLetter)
            {
                if (string.IsNullOrWhiteSpace(driveLetter))
                {
                    throw new ArgumentNullException(nameof(driveLetter));
                }

                var letter = driveLetter.TrimEnd(':', '\\');
                var tempPath = Path.Combine(letter + ":", "temp");

                try
                {
                    if (!Directory.Exists(tempPath))
                    {
                        Directory.CreateDirectory(tempPath);
                    }

                    TempBasePath = tempPath;
                    return tempPath;
                }
                catch (UnauthorizedAccessException ex)
                {
                    throw new UnauthorizedAccessException(
                        $"Insufficient permissions to create temp folder at {tempPath}. " +
                        $"Please run as administrator or choose a different drive.", ex);
                }
                catch (IOException ex)
                {
                    throw new IOException(
                        $"Failed to create temp folder at {tempPath}: {ex.Message}", ex);
                }
            }
        }

        // ===================================================================
        // DownloadTaskContext - Represents the state of a registered download task
        // ===================================================================
        private class DownloadTaskContext
        {
            public string TaskName { get; set; }
            public string DestinationPath { get; set; }
            public CancellationTokenSource CancellationTokenSource { get; set; }
            public ManualResetEventSlim PauseEvent { get; set; }
            public DateTime StartTime { get; set; }
            public bool IsPaused { get; set; }
        }

        // ===================================================================
        // DownloadRegistry - Global registry for all active downloads
        // ===================================================================
        private static class DownloadRegistry
        {
            private static readonly ConcurrentDictionary<string, DownloadTaskContext> _activeDownloads
                = new ConcurrentDictionary<string, DownloadTaskContext>();

            public static void Register(string taskId, DownloadTaskContext context)
            {
                _activeDownloads.TryAdd(taskId, context);
            }

            public static void Unregister(string taskId)
            {
                _activeDownloads.TryRemove(taskId, out _);
            }

            public static void PauseAll()
            {
                foreach (var kvp in _activeDownloads)
                {
                    try
                    {
                        var context = kvp.Value;
                        if (context != null && !context.IsPaused)
                        {
                            context.PauseEvent?.Reset();
                            context.IsPaused = true;
                        }
                    }
                    catch { }
                }
            }

            public static void ResumeAll()
            {
                foreach (var kvp in _activeDownloads)
                {
                    try
                    {
                        var context = kvp.Value;
                        if (context != null && context.IsPaused)
                        {
                            context.PauseEvent?.Set();
                            context.IsPaused = false;
                        }
                    }
                    catch { }
                }
            }

            public static void StopAll()
            {
                foreach (var kvp in _activeDownloads)
                {
                    try
                    {
                        var context = kvp.Value;
                        if (context?.CancellationTokenSource != null
                            && !context.CancellationTokenSource.IsCancellationRequested)
                        {
                            context.CancellationTokenSource.Cancel();
                            context.PauseEvent?.Set();
                        }
                    }
                    catch { }
                }
            }

            public static int ActiveCount => _activeDownloads.Count;

            public static void Clear()
            {
                _activeDownloads.Clear();
            }
        }

        // ===================================================================
        // DownloadProgressInfo - Progress info reported on every UI tick
        // ===================================================================
        private sealed class DownloadProgressInfo
        {
            public long BytesDone { get; set; }
            public long TotalBytes { get; set; }
            public double SpeedBytesPerSec { get; set; }
            public int[] SegmentPercents { get; set; }
            public int OverallPercent =>
                TotalBytes > 0 ? (int)Math.Min(100, BytesDone * 100L / TotalBytes) : 0;
        }

        // ===================================================================
        // SegmentedDownloadEngineOptimized - High-performance download engine
        // ===================================================================
        private sealed class SegmentedDownloadEngineOptimized
        {
            // Performance Tunables
            private const long ChunkSize = 256L * 1024;                    // 256 KB chunks
            private const long MinSizeForSegmented = 1L * 1024 * 1024;     // 1 MB minimum
            private const int MaxRedirects = 10;
            private const int MaxRetries = 10;
            private static readonly TimeSpan HeadTimeout = TimeSpan.FromSeconds(60);
            private static readonly TimeSpan ChunkTimeout = TimeSpan.FromHours(4);
            private const double EmaAlpha = 0.2;
            private const int NetworkBufferSize = 262144;                  // 256KB network buffer
            private const int FileBufferSize = 262144;                     // 256KB file buffer
            private const int MaxConcurrentConnections = 32;

            private static readonly string UserAgent =
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

            private static readonly ConcurrentQueue<byte[]> _bufferPool = new ConcurrentQueue<byte[]>();

            // State for dynamic segment changes
            private long _currentFileSize = 0;
            private long _currentDownloadedBytes = 0;
            private ConcurrentQueue<ChunkRange> _chunkQueue = null;
            private ConcurrentQueue<ChunkRange> _retryQueue = null;
            private int _currentSegments = 0;
            private ManualResetEventSlim _pauseEvent = null;
            private object _reallocLock = new object();

            static SegmentedDownloadEngineOptimized()
            {
                ServicePointManager.DefaultConnectionLimit = MaxConcurrentConnections;
                ServicePointManager.Expect100Continue = false;
                ServicePointManager.UseNagleAlgorithm = false;
                ServicePointManager.MaxServicePoints = 100;

                for (int i = 0; i < 32; i++)
                {
                    _bufferPool.Enqueue(new byte[NetworkBufferSize]);
                }
            }

            public async Task ReallocateSegmentsDuringDownloadAsync(int newSegmentCount)
            {
                if (_chunkQueue == null || _retryQueue == null || _pauseEvent == null)
                    return;

                lock (_reallocLock)
                {
                    _pauseEvent.Reset();
                }

                try
                {
                    await Task.Delay(200);

                    lock (_reallocLock)
                    {
                        long remainingBytes = _currentFileSize - _currentDownloadedBytes;

                        if (remainingBytes <= 0)
                        {
                            _pauseEvent.Set();
                            return;
                        }

                        while (_chunkQueue.TryDequeue(out _)) { }
                        while (_retryQueue.TryDequeue(out _)) { }

                        newSegmentCount = Math.Max(1, Math.Min(newSegmentCount, 32));

                        long regionSize = remainingBytes / Math.Max(1, newSegmentCount);
                        if (regionSize < ChunkSize) regionSize = ChunkSize;

                        long start = _currentDownloadedBytes;
                        int chunksAdded = 0;

                        for (int i = 0; i < newSegmentCount && start < _currentFileSize; i++)
                        {
                            long end = (i == newSegmentCount - 1) ? _currentFileSize - 1 : start + regionSize - 1;

                            while (start <= end && start < _currentFileSize)
                            {
                                long chunkEnd = Math.Min(start + ChunkSize - 1, end);
                                _chunkQueue.Enqueue(new ChunkRange
                                {
                                    Start = start,
                                    End = chunkEnd,
                                    Downloaded = 0
                                });
                                start = chunkEnd + 1;
                                chunksAdded++;
                            }
                        }

                        _currentSegments = newSegmentCount;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"ReallocateSegmentsDuringDownloadAsync error: {ex.Message}");
                }
                finally
                {
                    try
                    {
                        if (_pauseEvent != null)
                            _pauseEvent.Set();
                    }
                    catch { }
                }
            }

            public async Task DownloadAsync(string url, string destinationPath, int segments,
                IProgress<DownloadProgressInfo> progress, CancellationToken ct,
                ManualResetEventSlim pauseEvent = null)
            {
                if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));
                if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));
                segments = Math.Max(1, Math.Min(segments, 32));

                try
                {
                    var probe = await ProbeAsync(url, ct);

                    if (!probe.SupportsRange || probe.FileSize < MinSizeForSegmented)
                    {
                        await DownloadSingleOptimizedAsync(probe.FinalUrl, destinationPath, progress, ct, pauseEvent);
                        return;
                    }

                    await DownloadSegmentedOptimizedAsync(probe.FinalUrl, destinationPath, probe.FileSize, segments, progress, ct, pauseEvent);
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

            public async Task DownloadSingleFastAsync(string url, string destinationPath,
                IProgress<DownloadProgressInfo> progress, CancellationToken ct,
                ManualResetEventSlim pauseEvent = null)
            {
                if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));
                if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));

                try
                {
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

            public async Task DownloadMultiSegmentFastAsync(string url, string destinationPath, int segments,
                IProgress<DownloadProgressInfo> progress, CancellationToken ct,
                ManualResetEventSlim pauseEvent = null)
            {
                if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));
                if (string.IsNullOrWhiteSpace(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));
                segments = Math.Max(1, Math.Min(segments, 32));

                try
                {
                    await DownloadSegmentedOptimizedAsync(url, destinationPath, 0, segments, progress, ct, pauseEvent);
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

            private struct ProbeResult
            {
                public string FinalUrl;
                public long FileSize;
                public bool SupportsRange;
            }

            private async Task<ProbeResult> ProbeAsync(string url, CancellationToken ct)
            {
                var result = new ProbeResult { FinalUrl = url, FileSize = 0, SupportsRange = false };

                try
                {
                    using (var handler = new HttpClientHandler
                    {
                        AllowAutoRedirect = true,
                        UseCookies = false,
                        AutomaticDecompression = DecompressionMethods.None
                    })
                    using (var client = new HttpClient(handler) { Timeout = HeadTimeout })
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                        client.DefaultRequestHeaders.Add("Accept", "*/*");

                        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                        {
                            cts.CancelAfter(HeadTimeout);

                            var request = new HttpRequestMessage(HttpMethod.Head, url);
                            var response = await client.SendAsync(request,
                                HttpCompletionOption.ResponseHeadersRead, cts.Token);

                            result.FinalUrl = response.RequestMessage?.RequestUri?.AbsoluteUri ?? url;
                            result.FileSize = response.Content.Headers.ContentLength ?? 0;
                            result.SupportsRange = response.Headers.AcceptRanges.Contains("bytes");
                        }
                    }
                }
                catch { }

                if (result.FileSize > 0 && result.SupportsRange) return result;

                try
                {
                    string probeUrl = result.FinalUrl;

                    using (var handler = new HttpClientHandler
                    {
                        AllowAutoRedirect = false,
                        UseCookies = false,
                        AutomaticDecompression = DecompressionMethods.None
                    })
                    using (var client = new HttpClient(handler) { Timeout = HeadTimeout })
                    {
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
                catch { }

                return result;
            }

            private async Task DownloadSingleOptimizedAsync(string url, string destinationPath,
                IProgress<DownloadProgressInfo> progress, CancellationToken ct,
                ManualResetEventSlim pauseEvent)
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
                                    byte[] buffer = GetBuffer();

                                    try
                                    {
                                        int read;
                                        var lastReport = DateTime.Now;

                                        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                                        {
                                            if (pauseEvent != null && !pauseEvent.IsSet)
                                            {
                                                await Task.Run(() => pauseEvent.Wait(cts.Token), cts.Token);
                                            }

                                            await fs.WriteAsync(buffer, 0, read, cts.Token);
                                            done += read;

                                            var now = DateTime.Now;
                                            double elapsed = (now - lastTime).TotalSeconds;
                                            if (elapsed >= 0.2)
                                            {
                                                double rawSpeed = (done - lastBytes) / elapsed;
                                                speed = speed == 0 ? rawSpeed : EmaAlpha * rawSpeed + (1 - EmaAlpha) * speed;
                                                lastBytes = done;
                                                lastTime = now;
                                            }

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
                        return;
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

            private async Task<long> DiscoverFileSizeAsync(string url, CancellationToken ct)
            {
                try
                {
                    using (var handler = new HttpClientHandler
                    {
                        UseCookies = false,
                        AutomaticDecompression = DecompressionMethods.None
                    })
                    using (var client = new HttpClient(handler) { Timeout = HeadTimeout })
                    {
                        client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
                        client.DefaultRequestHeaders.Add("Accept", "*/*");

                        using (var cts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                        {
                            cts.CancelAfter(HeadTimeout);
                            var request = new HttpRequestMessage(HttpMethod.Head, url);
                            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                            if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
                            {
                                return response.Content.Headers.ContentLength.Value;
                            }

                            response.Dispose();
                            request = new HttpRequestMessage(HttpMethod.Get, url);
                            request.Headers.Range = new RangeHeaderValue(0, 0);
                            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                            if (response.StatusCode == HttpStatusCode.PartialContent &&
                                response.Content.Headers.ContentRange?.Length.HasValue == true)
                            {
                                return response.Content.Headers.ContentRange.Length.Value;
                            }
                            else if (response.IsSuccessStatusCode && response.Content.Headers.ContentLength.HasValue)
                            {
                                return response.Content.Headers.ContentLength.Value;
                            }
                        }
                    }
                }
                catch { }
                return 0;
            }

            private async Task DownloadSegmentedOptimizedAsync(string url, string destinationPath,
                long fileSize, int segments, IProgress<DownloadProgressInfo> progress, CancellationToken ct,
                ManualResetEventSlim pauseEvent)
            {
                if (fileSize <= 0)
                {
                    fileSize = await DiscoverFileSizeAsync(url, ct);
                    if (fileSize <= 0)
                    {
                        await DownloadSingleOptimizedAsync(url, destinationPath, progress, ct, pauseEvent);
                        return;
                    }
                }

                using (var fs = new FileStream(destinationPath, FileMode.Create, FileAccess.Write,
                    FileShare.ReadWrite, FileBufferSize, FileOptions.Asynchronous))
                {
                    fs.SetLength(fileSize);
                }

                _chunkQueue = new ConcurrentQueue<ChunkRange>();
                _retryQueue = new ConcurrentQueue<ChunkRange>();
                _currentFileSize = fileSize;
                _currentDownloadedBytes = 0;
                _currentSegments = segments;
                _pauseEvent = pauseEvent;

                long regionSize = fileSize / segments;
                var regionProgress = new long[segments];
                var regionStarts = new long[segments];
                var regionEnds = new long[segments];

                for (int i = 0; i < segments; i++)
                {
                    regionStarts[i] = i * regionSize;
                    regionEnds[i] = (i == segments - 1) ? fileSize - 1 : (i + 1) * regionSize - 1;
                }

                bool hasMore = true;
                while (hasMore)
                {
                    hasMore = false;
                    for (int i = 0; i < segments; i++)
                    {
                        if (regionStarts[i] <= regionEnds[i])
                        {
                            long end = Math.Min(regionStarts[i] + ChunkSize - 1, regionEnds[i]);
                            _chunkQueue.Enqueue(new ChunkRange
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

                var progressState = new ProgressState
                {
                    TotalDownloaded = 0,
                    SmoothedSpeed = 0,
                    LastSpeedBytes = 0,
                    LastSpeedTime = DateTime.Now,
                    Lock = new object()
                };

                var workers = new Task[segments];
                for (int i = 0; i < segments; i++)
                {
                    int workerIndex = i;
                    workers[i] = Task.Run(async () =>
                    {
                        await ProcessChunksAsync(url, destinationPath, _chunkQueue, _retryQueue,
                            workerIndex, regionProgress, progressState, ct, pauseEvent);
                    }, ct);
                }

                using (var uiCts = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    var uiTask = Task.Run(async () =>
                    {
                        while (!uiCts.Token.IsCancellationRequested)
                        {
                            try { await Task.Delay(100, uiCts.Token); } catch { break; }

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
                        await Task.WhenAll(workers);
                        try { await Task.Delay(200, ct); } catch { }
                    }
                    finally
                    {
                        uiCts.Cancel();
                        try { await uiTask.ConfigureAwait(false); } catch { }
                    }
                }
            }

            private sealed class ProgressState
            {
                public long TotalDownloaded;
                public double SmoothedSpeed;
                public long LastSpeedBytes;
                public DateTime LastSpeedTime;
                public object Lock;
            }

            private async Task ProcessChunksAsync(string url, string destinationPath,
                ConcurrentQueue<ChunkRange> chunkQueue, ConcurrentQueue<ChunkRange> retryQueue,
                int regionIndex, long[] regionProgress, ProgressState progressState, CancellationToken ct,
                ManualResetEventSlim pauseEvent)
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

                    using (var fs = new FileStream(destinationPath, FileMode.Open, FileAccess.Write,
                        FileShare.ReadWrite, FileBufferSize, FileOptions.Asynchronous | FileOptions.RandomAccess))
                    {
                        byte[] buffer = GetBuffer();

                        try
                        {
                            while (!ct.IsCancellationRequested)
                            {
                                if (pauseEvent != null && !pauseEvent.IsSet)
                                {
                                    await Task.Run(() => pauseEvent.Wait(ct), ct);
                                }

                                ChunkRange chunk;

                                if (!_chunkQueue.TryDequeue(out chunk))
                                {
                                    if (!_retryQueue.TryDequeue(out chunk))
                                        break;
                                }

                                try
                                {
                                    string currentUrl = url;

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
                                                    fs.Seek(chunk.Start + chunk.Downloaded, SeekOrigin.Begin);

                                                    int read;

                                                    while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cts.Token)) > 0)
                                                    {
                                                        await fs.WriteAsync(buffer, 0, read, cts.Token);
                                                        chunk.Downloaded += read;

                                                        lock (progressState.Lock)
                                                        {
                                                            progressState.TotalDownloaded += read;
                                                            _currentDownloadedBytes += read;
                                                            regionProgress[regionIndex] += read;

                                                            var now = DateTime.Now;
                                                            double elapsed = (now - progressState.LastSpeedTime).TotalSeconds;
                                                            if (elapsed >= 0.1)
                                                            {
                                                                double rawSpeed = (progressState.TotalDownloaded - progressState.LastSpeedBytes) / elapsed;
                                                                progressState.SmoothedSpeed = progressState.SmoothedSpeed == 0
                                                                    ? rawSpeed
                                                                    : EmaAlpha * rawSpeed + (1 - EmaAlpha) * progressState.SmoothedSpeed;
                                                                progressState.LastSpeedBytes = progressState.TotalDownloaded;
                                                                progressState.LastSpeedTime = now;
                                                            }
                                                        }
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

            private static void CleanupPartialDownload(string destinationPath)
            {
                Task.Run(() =>
                {
                    try
                    {
                        if (File.Exists(destinationPath))
                        {
                            int maxRetries = 5;
                            int retryDelayMs = 100;

                            for (int attempt = 0; attempt < maxRetries; attempt++)
                            {
                                try
                                {
                                    File.Delete(destinationPath);
                                    break;
                                }
                                catch (IOException)
                                {
                                    if (attempt < maxRetries - 1)
                                    {
                                        Thread.Sleep(retryDelayMs);
                                        retryDelayMs *= 2;
                                    }
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    break;
                                }
                            }
                        }
                    }
                    catch { }
                });
            }

            private sealed class ChunkRange
            {
                public long Start;
                public long End;
                public long Downloaded;
                public long Length => End - Start + 1;
            }
        }
        // ===================================================================
        // Current Download Info - Để track download hiện tại cho việc restart
        // ===================================================================
        private class CurrentDownloadInfo
        {
            public string DownloadUrl { get; set; }
            public string DestinationPath { get; set; }
            public string DisplayName { get; set; }
            public bool UseProbe { get; set; }  // true = DownloadWithProgressAsync, false = DownloadSingleLinkFastAsync
        }

        private CurrentDownloadInfo _currentDownloadInfo = null;

        // Track the active download engine so UI can call ReallocateSegmentsDuringDownload()
        private SegmentedDownloadEngineOptimized _activeDownloadEngine = null;

        // Fields cho download
        private CancellationTokenSource _cancellationTokenSource;
        private ManualResetEventSlim _pauseEvent = new ManualResetEventSlim(true);
        private SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(1);

        // IsDownloading property đã có trong MainWindow.SystemBar.cs
        // Các hàm FormatSpeed, FormatBytes cũng đã có trong MainWindow.SystemBar.cs

        // ===================================================================
        // DOWNLOAD WITH RETRY - For unstable connections
        // ===================================================================
        private async Task DownloadWithRetryAsync(string downloadUrl, string destinationPath, string displayName, int maxRetries = 5)
        {
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    await DownloadSingleLinkFastAsync(downloadUrl, destinationPath, displayName);
                    return; // Success
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        UpdateStatus($"Failed to download {displayName} after {maxRetries} attempts: {ex.Message}", "Red");
                        throw;
                    }
                    UpdateStatus($"Download retry {retryCount}/{maxRetries} for {displayName}: {ex.Message}", "Yellow");
                    await Task.Delay(1000 * retryCount);
                }
            }
        }

        // ===================================================================
        // FAST SINGLE-LINK DOWNLOAD - "Golden Standard"
        // ===================================================================
        // Sử dụng cho: TẤT CẢ checkbox tải đơn link (1 file .exe, .msi)
        // Ví dụ: VPN1111, MMT Apps, Google Drive, DISM++, NetLimiter, etc.
        //
        // Tại sao nhanh? Bỏ qua ProbeAsync (HEAD request) → tải ngay lập tức
        // Số segment: Lấy từ CboSegmentCount (user chọn)
        // ===================================================================
        private async Task DownloadSingleLinkFastAsync(string downloadUrl, string destinationPath, string displayName)
        {
            await _downloadSemaphore.WaitAsync();
            try
            {
                var ct = _cancellationTokenSource?.Token ?? CancellationToken.None;

                // Get segment count from CboSegmentCount (user selection)
                int segments = 16; // Default to 16 segments for optimal performance
                await Dispatcher.InvokeAsync(() =>
                {
                    if (CboSegmentCount?.SelectedItem is ComboBoxItem item &&
                        int.TryParse(item.Content?.ToString(), out int n))
                        segments = n;
                });

                Progress<DownloadProgressInfo> uiProgress = null;
                await Dispatcher.InvokeAsync(() =>
                {
                    ResetDownloadUI();
                    uiProgress = new Progress<DownloadProgressInfo>(info =>
                    {
                        if (!ct.IsCancellationRequested)
                            ApplyDownloadProgressToUI(info);
                    });
                });

                UpdateStatus($"Đang tải {displayName}... ({segments} threads)", "Cyan");

                // Create download context for global registry
                var taskContext = new DownloadTaskContext
                {
                    TaskName = displayName,
                    DestinationPath = destinationPath,
                    CancellationTokenSource = _cancellationTokenSource,
                    PauseEvent = _pauseEvent,
                    StartTime = DateTime.Now,
                    IsPaused = false
                };

                // Register in global registry (for cross-tab Pause/Stop)
                DownloadRegistry.Register(destinationPath, taskContext);

                try
                {
                    // FAST PATH + MULTI-SEGMENT: Skip probe, download with selected segments
                    var engine = new SegmentedDownloadEngineOptimized();
                    await engine.DownloadMultiSegmentFastAsync(downloadUrl, destinationPath, segments, uiProgress, ct, _pauseEvent);

                    await Dispatcher.InvokeAsync(() => ResetDownloadUI());
                }
                finally
                {
                    DownloadRegistry.Unregister(destinationPath);
                }
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi tải: {ex.Message}", "Red");
                throw;
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        }

        // ===================================================================
        // MULTI-SEGMENT DOWNLOAD (Có Probe)
        // ===================================================================
        // Sử dụng cho: File lớn từ Archive.org, Mediafire (cần dò redirect)
        // Ví dụ: Win10/Win11 ISO, file từ Mediafire links
        //
        // Lưu ý: Chậm hơn SingleLink vì có ProbeAsync (HEAD request)
        // ===================================================================
        private async Task DownloadWithProgressAsync(string downloadUrl, string destinationPath, string displayName = "File")
        {
            await _downloadSemaphore.WaitAsync();
            try
            {
                // LƯU download info để có thể restart nếu user đổi segment
                _currentDownloadInfo = new CurrentDownloadInfo
                {
                    DownloadUrl = downloadUrl,
                    DestinationPath = destinationPath,
                    DisplayName = displayName,
                    UseProbe = true  // Đây là DownloadWithProgressAsync (có probe)
                };

                var ct = _cancellationTokenSource?.Token ?? CancellationToken.None;

                int segments = 16; // Default to 16 segments for optimal performance
                await Dispatcher.InvokeAsync(() =>
                {
                    if (CboSegmentCount?.SelectedItem is ComboBoxItem item &&
                        int.TryParse(item.Content?.ToString(), out int n))
                        segments = n;
                });

                Progress<DownloadProgressInfo> uiProgress = null;
                await Dispatcher.InvokeAsync(() =>
                {
                    ResetDownloadUI();
                    uiProgress = new Progress<DownloadProgressInfo>(info =>
                    {
                        if (!ct.IsCancellationRequested)
                            ApplyDownloadProgressToUI(info);
                    });
                });

                UpdateStatus($"Dang tai {displayName}... ({segments} threads)", "Cyan");

                // Create download context for global registry
                var taskContext = new DownloadTaskContext
                {
                    TaskName = displayName,
                    DestinationPath = destinationPath,
                    CancellationTokenSource = _cancellationTokenSource,
                    PauseEvent = _pauseEvent,
                    StartTime = DateTime.Now,
                    IsPaused = false
                };

                // Register in global registry
                DownloadRegistry.Register(destinationPath, taskContext);

                try
                {
                    // Use optimized download engine with probe (for Archive.org, Mediafire)
                    _activeDownloadEngine = new SegmentedDownloadEngineOptimized();
                    await _activeDownloadEngine.DownloadAsync(downloadUrl, destinationPath, segments, uiProgress, ct, _pauseEvent);

                    await Dispatcher.InvokeAsync(() => ResetDownloadUI());
                }
                finally
                {
                    _activeDownloadEngine = null;
                    DownloadRegistry.Unregister(destinationPath);
                }
                return;
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                UpdateStatus($"Loi tai: {ex.Message}", "Red");
                throw;
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        }

        // ===================================================================
        // MULTI-PART DOWNLOAD (Nhiều file .part)
        // ===================================================================
        // Sử dụng cho: Game nhiều phần (Ghost of Tsushima 29 parts, Samurai Maiden 4 parts)
        // Mỗi part được tải tuần tự với 16 segments, tất cả vào cùng 1 thư mục
        // ===================================================================
        private async Task DownloadMultiPartAsync(
            string[] downloadUrls,
            string[] destinationPaths,
            string displayNamePrefix)
        {
            for (int i = 0; i < downloadUrls.Length; i++)
            {
                string partName = $"{displayNamePrefix} - Part {i + 1}/{downloadUrls.Length}";
                UpdateStatus($"Đang tải {partName}...", "Cyan");

                await DownloadSingleLinkFastAsync(downloadUrls[i], destinationPaths[i], partName);

                // Reset UI between parts
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });
            }
        }

        // ===================================================================
        // HELPER: Reset Download UI
        // ===================================================================
        private void ResetDownloadUI()
        {
            DownloadProgressBar.Value = 0;
            DownloadProgressBar.Visibility = Visibility.Visible;
            ConnectionTraceGrid.Children.Clear();
            ProgressTextBlock.Text = "";
            SpeedTextBlock.Text = "";
            ConnectionCountTextBlock.Text = "";
        }

        // ===================================================================
        // HELPER: Apply Download Progress to UI
        // ===================================================================
        private void ApplyDownloadProgressToUI(DownloadProgressInfo info)
        {
            // Always show main progress bar AND segment bars simultaneously
            DownloadProgressBar.Visibility = Visibility.Visible;

            // Update main progress bar with overall percentage
            DownloadProgressBar.Value = info.OverallPercent;

            // Update speed and progress text
            SpeedTextBlock.Text = FormatSpeed(info.SpeedBytesPerSec);
            ProgressTextBlock.Text = info.TotalBytes > 0
                ? $"{FormatBytes(info.BytesDone)} / {FormatBytes(info.TotalBytes)}"
                : FormatBytes(info.BytesDone);

            // Update segment bars
            if (info.SegmentPercents != null && info.SegmentPercents.Length > 1)
            {
                int count = info.SegmentPercents.Length;
                if (ConnectionTraceGrid.Children.Count != count)
                {
                    ConnectionTraceGrid.Children.Clear();
                    UpdateConnectionTraceOrientation();
                    for (int s = 0; s < count; s++)
                        ConnectionTraceGrid.Children.Add(new ProgressBar
                        {
                            Minimum = 0, Maximum = 100, Value = 0,
                            Style = (Style)FindResource("RoundedProgressBarStyle"),
                            Margin = new Thickness(1, 0, 1, 0)
                        });
                }
                for (int s = 0; s < count && s < ConnectionTraceGrid.Children.Count; s++)
                    if (ConnectionTraceGrid.Children[s] is ProgressBar pb)
                        pb.Value = info.SegmentPercents[s];
                ConnectionCountTextBlock.Text = $"Threads: {count}";
            }
        }

        // FormatSpeed và FormatBytes đã có trong MainWindow.SystemBar.cs
    }
}
