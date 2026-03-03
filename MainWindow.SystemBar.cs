using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        private class DownloadRange
        {
            public long Start { get; set; }
            public long End { get; set; }
            public long Downloaded { get; set; }
            public long Length => End - Start + 1;
        }

        private class WorkerData
        {
            public int Index;
            public ProgressBar ProgressBar;
        }

        /// <summary>
        /// Download với retry logic - dùng cho các nguồn không ổn định (OneDrive, MediaFire)
        /// Tự động retry khi connection stalled hoặc empty reads
        /// </summary>
        private async Task DownloadWithRetryAsync(string downloadUrl, string destinationPath, string displayName, int maxRetries = 5)
        {
            int retryCount = 0;
            int stallCount = 0;
            const int STALL_THRESHOLD_SECONDS = 30; // 30 giây không có progress = stalled (tăng từ 10 lên 30 cho OneDrive)
            const int MONITOR_INTERVAL_MS = 3000; // Check mỗi 3 giây (giảm từ 1s xuống để tránh spam OneDrive)

            while (retryCount < maxRetries)
            {
                try
                {
                    // Tạo cancellation token mới cho mỗi lần thử
                    using (var retryCts = new CancellationTokenSource())
                    {
                        var downloadTask = DownloadWithProgressAsync(downloadUrl, destinationPath, displayName);
                        
                        // Monitor progress trong khi download - giảm frequency để tránh spam OneDrive
                        var monitorTask = Task.Run(async () =>
                        {
                            DateTime lastProgressTime = DateTime.Now;
                            
                            while (!downloadTask.IsCompleted && !downloadTask.IsFaulted)
                            {
                                await Task.Delay(MONITOR_INTERVAL_MS, retryCts.Token).ConfigureAwait(false);
                                
                                var now = DateTime.Now;
                                if ((now - lastProgressTime).TotalSeconds > STALL_THRESHOLD_SECONDS)
                                {
                                    stallCount++;
                                    if (stallCount >= 2) // 2 lần check stalled = cancel (60 giây total)
                                    {
                                        try { retryCts.Cancel(); } catch { }
                                        break;
                                    }
                                }
                                else
                                {
                                    stallCount = 0;
                                    lastProgressTime = now;
                                }
                            }
                        }, retryCts.Token);

                        await downloadTask;
                        
                        // Nếu hoàn thành thành công, thoát loop
                        if (downloadTask.IsCompleted && !downloadTask.IsFaulted && !downloadTask.IsCanceled)
                            return;
                    }
                }
                catch (OperationCanceledException)
                {
                    retryCount++;
                    UpdateStatus($"Connection stalled. Đang retry lần {retryCount}/{maxRetries}...", "Yellow");
                    
                    // Xóa file dở dang để tải lại từ đầu
                    if (File.Exists(destinationPath))
                    {
                        try { File.Delete(destinationPath); } catch { }
                    }
                    
                    // Delay trước khi retry
                    await Task.Delay(3000 * retryCount); // Tăng delay theo số lần retry
                }
                catch (Exception ex)
                {
                    retryCount++;
                    UpdateStatus($"Lỗi tải (lần {retryCount}/{maxRetries}): {ex.Message}", "Yellow");
                    
                    if (File.Exists(destinationPath))
                    {
                        try { File.Delete(destinationPath); } catch { }
                    }
                    
                    await Task.Delay(3000 * retryCount);
                }
            }

            throw new Exception($"Tải file thất bại sau {maxRetries} lần thử. Vui lòng kiểm tra kết nối mạng.");
        }

        /// <summary>
        /// Download đặc biệt cho OneDrive/SharePoint - Single threaded, không stall detection, không multi-thread
        /// Chỉ tải đơn giản với progress reporting
        /// </summary>
        private async Task DownloadOneDriveAsync(string downloadUrl, string destinationPath, string displayName)
        {
            const int MAX_RETRIES = 3;
            int retryCount = 0;

            while (retryCount < MAX_RETRIES)
            {
                try
                {
                    UpdateStatus($"Đang tải {displayName}... (lần {retryCount + 1}/{MAX_RETRIES})", "Cyan");

                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromHours(2); // Timeout dài cho file lớn
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                        using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
                        {
                            response.EnsureSuccessStatusCode();

                            long? totalBytes = response.Content.Headers.ContentLength;
                            
                            using (var contentStream = await response.Content.ReadAsStreamAsync())
                            using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
                            {
                                byte[] buffer = new byte[81920]; // 80KB buffer
                                long totalRead = 0;
                                int read;

                                while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                {
                                    await fileStream.WriteAsync(buffer, 0, read);
                                    totalRead += read;

                                    // Update progress mỗi 512KB để tránh spam UI
                                    if (totalRead % (512 * 1024) < 81920)
                                    {
                                        Dispatcher.InvokeAsync(() =>
                                        {
                                            if (totalBytes.HasValue && totalBytes.Value > 0)
                                            {
                                                double percent = (double)totalRead / totalBytes.Value * 100;
                                                DownloadProgressBar.Value = percent;
                                                ProgressTextBlock.Text = $"{FormatBytes(totalRead)} / {FormatBytes(totalBytes.Value)}";
                                            }
                                            else
                                            {
                                                ProgressTextBlock.Text = $"{FormatBytes(totalRead)} downloaded";
                                            }
                                        });
                                    }
                                }

                                Dispatcher.InvokeAsync(() =>
                                {
                                    DownloadProgressBar.Value = 100;
                                    if (totalBytes.HasValue && totalBytes.Value > 0)
                                        ProgressTextBlock.Text = $"{FormatBytes(totalBytes.Value)} / {FormatBytes(totalBytes.Value)}";
                                });
                            }
                        }
                    }

                    UpdateStatus($"Tải xong {displayName}!", "Green");
                    return; // Thành công, thoát loop
                }
                catch (Exception ex)
                {
                    retryCount++;
                    UpdateStatus($"Lỗi tải OneDrive (lần {retryCount}/{MAX_RETRIES}): {ex.Message}", "Yellow");
                    
                    if (retryCount < MAX_RETRIES)
                    {
                        await Task.Delay(3000 * retryCount); // Delay tăng dần
                    }
                    
                    // Xóa file dở dang
                    if (File.Exists(destinationPath))
                    {
                        try { File.Delete(destinationPath); } catch { }
                    }
                }
            }

            throw new Exception($"Tải OneDrive thất bại sau {MAX_RETRIES} lần thử.");
        }

        public bool IsDownloading { get; private set; }

        private async Task DownloadWithProgressAsync(string downloadUrl, string destinationPath, string displayName = "File")
        {
            if (IsDownloading)
                throw new InvalidOperationException("Tiến trình trước đó chưa kết thúc hoàn toàn. Vui lòng chờ 1-2 giây rồi thử lại.");

            IsDownloading = true;
            try
            {
                var ct = _cancellationTokenSource?.Token ?? CancellationToken.None;
                int maxRetries = 10;
                int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    // Step 1: HEAD probe with full redirect follow to get final URL + file size
                    var headHandler = new HttpClientHandler { AllowAutoRedirect = true };
                    string finalUrl = downloadUrl;
                    long fileSize = 0;
                    bool supportsRanges = false;

                    using (var headClient = new HttpClient(headHandler))
                    using (var headTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                    using (var headLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(headTimeoutCts.Token, ct))
                    {
                        headClient.Timeout = TimeSpan.FromSeconds(60);
                        headClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                        try
                        {
                            var headResp = await headClient.SendAsync(
                                new HttpRequestMessage(HttpMethod.Head, downloadUrl),
                                HttpCompletionOption.ResponseHeadersRead, headLinkedCts.Token);
                            finalUrl = headResp.RequestMessage?.RequestUri?.AbsoluteUri ?? downloadUrl;
                            fileSize = headResp.Content.Headers.ContentLength ?? 0;
                            supportsRanges = headResp.Headers.AcceptRanges.Contains("bytes");
                        }
                        catch { /* HEAD unsupported — will probe with GET below */ }
                    }

                    // Step 2: If HEAD didn't give us size or range info, probe with GET Range:0-0 on final URL
                    if (fileSize == 0 || !supportsRanges)
                    {
                        var probeHandler = new HttpClientHandler { AllowAutoRedirect = false };
                        using (var probeClient = new HttpClient(probeHandler))
                        using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(60)))
                        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, ct))
                        {
                            probeClient.Timeout = TimeSpan.FromSeconds(60);
                            probeClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                            HttpResponseMessage probeResponse = null;
                            string currentProbeUrl = finalUrl;

                            for (int rd = 0; rd < 10; rd++)
                            {
                                var probeRequest = new HttpRequestMessage(HttpMethod.Get, currentProbeUrl);
                                probeRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
                                probeResponse = await probeClient.SendAsync(probeRequest, HttpCompletionOption.ResponseHeadersRead, linkedCts.Token);

                                int statusCode = (int)probeResponse.StatusCode;
                                if (statusCode >= 300 && statusCode <= 399 && probeResponse.Headers.Location != null)
                                {
                                    currentProbeUrl = probeResponse.Headers.Location.IsAbsoluteUri ? probeResponse.Headers.Location.AbsoluteUri : new Uri(new Uri(currentProbeUrl), probeResponse.Headers.Location).AbsoluteUri;
                                    probeResponse.Dispose();
                                    probeResponse = null;
                                }
                                else break;
                            }

                            using (probeResponse)
                            {
                                if (probeResponse != null)
                                {
                                    if (probeResponse.IsSuccessStatusCode || probeResponse.StatusCode == System.Net.HttpStatusCode.PartialContent)
                                    {
                                        if (fileSize == 0)
                                            fileSize = probeResponse.Content.Headers.ContentRange?.Length ?? probeResponse.Content.Headers.ContentLength ?? 0;
                                        supportsRanges = probeResponse.StatusCode == System.Net.HttpStatusCode.PartialContent;
                                        finalUrl = currentProbeUrl;
                                    }
                                    // If probe fails (e.g. 404), just fall through to single connection
                                }
                            }
                        }
                    }

                    using (HttpClient client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }))
                    {
                        client.Timeout = TimeSpan.FromMinutes(60);
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                        if (!supportsRanges || fileSize < 5 * 1024 * 1024)
                        {
                            await DownloadSingleConnectionAsync(downloadUrl, destinationPath, displayName);
                            return;
                        }

                        // Create placeholder
                        using (var placeholder = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | System.IO.FileShare.Delete))
                            placeholder.SetLength(fileSize);

                        long totalDownloaded = 0;
                        object lockObj = new object();

                        // Trạng thái cho việc hiển thị Segment - Chuyển lên đây để UI Task có thể truy cập
                        long[] regionDownloaded = null;
                        int lastNumConnectionsForUI = -1;
                        object regionLock = new object();
                        var workers = new List<WorkerData>();

                        // Speed tracking
                        long lastTotalForSpeed = 0;
                        DateTime lastSpeedUpdate = DateTime.Now;
                        double smoothedSpeed = 0.0;
                        const double emaAlpha = 0.25;
                        string speedText = "0 B/s";

                        // Auto-recovery tracking - Tăng threshold để tránh kích hoạt sai
                        DateTime lastProgressTime = DateTime.Now;
                        long lastProgressBytes = 0;
                        int stallCount = 0;
                        const int MAX_STALL_COUNT = 30; // ~6 seconds without progress = stall (tăng từ 6 lên 30)
                        const double MIN_SPEED_THRESHOLD = 100; // 100 B/s minimum speed (giảm từ 1024 xuống)

                        // UI Update Task
                        var uiUpdateCts = new CancellationTokenSource();
                        var uiUpdateTask = Task.Run(async () =>
                        {
                            while (!uiUpdateCts.Token.IsCancellationRequested)
                            {
                                try
                                {
                                    await Task.Delay(200, uiUpdateCts.Token);
                                    var now = DateTime.Now;
                                    long currentTotal;
                                    lock (lockObj) { currentTotal = totalDownloaded; }

                                    if ((now - lastSpeedUpdate).TotalMilliseconds >= 250)
                                    {
                                        double rawSpeed = (currentTotal - lastTotalForSpeed) / (now - lastSpeedUpdate).TotalSeconds;
                                        smoothedSpeed = smoothedSpeed == 0.0 ? rawSpeed : emaAlpha * rawSpeed + (1.0 - emaAlpha) * smoothedSpeed;
                                        speedText = FormatSpeed(smoothedSpeed);
                                        lastTotalForSpeed = currentTotal;
                                        lastSpeedUpdate = now;
                                        
                                        // Check for stall/speed drop
                                        if (currentTotal > lastProgressBytes)
                                        {
                                            lastProgressBytes = currentTotal;
                                            lastProgressTime = now;
                                            stallCount = 0;
                                        }
                                        else
                                        {
                                            stallCount++;
                                            // Auto-recovery disabled to prevent infinite loop on OneDrive/MediaFire
                                            // Only manual pause/resume can trigger re-segmentation
                                            if (stallCount >= MAX_STALL_COUNT && smoothedSpeed < MIN_SPEED_THRESHOLD)
                                            {
                                                // Just notify user, don't auto-recover
                                                await Dispatcher.InvokeAsync(() =>
                                                {
                                                    UpdateStatus($"Tốc độ tải quá thấp ({speedText}). Nếu tiếp tục, vui lòng Pause và Resume.", "Yellow");
                                                });
                                            }
                                        }
                                    }

                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        if (!ct.IsCancellationRequested)
                                        {
                                            DownloadProgressBar.Visibility = Visibility.Collapsed;
                                            SpeedTextBlock.Text = speedText;
                                            ProgressTextBlock.Text = $"{FormatBytes(currentTotal)} / {FormatBytes(fileSize)}";

                                            // Cập nhật tất cả các Segment ProgressBar cùng một lúc ở đây
                                            lock (regionLock)
                                            {
                                                if (regionDownloaded != null && workers != null && lastNumConnectionsForUI > 0)
                                                {
                                                    long rSize = fileSize / lastNumConnectionsForUI;
                                                    for (int i = 0; i < workers.Count && i < regionDownloaded.Length; i++)
                                                    {
                                                        long rStart = i * rSize;
                                                        long rEnd = (i == lastNumConnectionsForUI - 1) ? fileSize - 1 : (rStart + rSize - 1);
                                                        int rPct = (int)(regionDownloaded[i] * 100 / (rEnd - rStart + 1));
                                                        if (workers[i].ProgressBar != null)
                                                            workers[i].ProgressBar.Value = Math.Min(100, rPct);
                                                    }
                                                }
                                            }
                                        }
                                    });
                                }
                                catch { }
                            }
                        });

                        try
                        {
                            // ─── QUEUE-BASED WORK STEALING ENGINE (INTERLEAVED) ───
                            const long ChunkSize = 8 * 1024 * 1024; // 8MB chunks
                            var chunkQueue = new ConcurrentQueue<DownloadRange>();

                            int numConnectionsForQueue = 16;
                            await Dispatcher.InvokeAsync(() =>
                            {
                                if (CboSegmentCount?.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out int count))
                                    numConnectionsForQueue = count;
                                else if (int.TryParse(CboSegmentCount?.SelectedValue?.ToString(), out int count2))
                                    numConnectionsForQueue = count2;
                            });

                            long regionSizeForQueue = fileSize / numConnectionsForQueue;
                            long[] regionStarts = new long[numConnectionsForQueue];
                            long[] regionEnds = new long[numConnectionsForQueue];
                            for (int i = 0; i < numConnectionsForQueue; i++)
                            {
                                regionStarts[i] = i * regionSizeForQueue;
                                regionEnds[i] = (i == numConnectionsForQueue - 1) ? fileSize - 1 : (i + 1) * regionSizeForQueue - 1;
                            }

                            bool workRemaining = true;
                            while (workRemaining)
                            {
                                workRemaining = false;
                                for (int i = 0; i < numConnectionsForQueue; i++)
                                {
                                    if (regionStarts[i] <= regionEnds[i])
                                    {
                                        long chunkEnd = Math.Min(regionStarts[i] + ChunkSize - 1, regionEnds[i]);
                                        chunkQueue.Enqueue(new DownloadRange { Start = regionStarts[i], End = chunkEnd, Downloaded = 0 });
                                        regionStarts[i] = chunkEnd + 1;
                                        workRemaining = true;
                                    }
                                }
                            }

                            // Keep visualization state outside loop to persist through Pause/Resume

                            while (!chunkQueue.IsEmpty)
                            {
                                await Task.Run(() => _pauseEvent.Wait(ct));
                                ct.ThrowIfCancellationRequested();

                                if (_pauseCts == null || _pauseCts.IsCancellationRequested)
                                    _pauseCts = new CancellationTokenSource();

                                int numConnections = 16;
                                await Dispatcher.InvokeAsync(() =>
                                {
                                    if (CboSegmentCount?.SelectedItem is ComboBoxItem item && int.TryParse(item.Content?.ToString(), out int count))
                                        numConnections = count;
                                    else if (int.TryParse(CboSegmentCount?.SelectedValue?.ToString(), out int count2))
                                        numConnections = count2;
                                });

                                if (_isReSegmenting)
                                {
                                    await Dispatcher.InvokeAsync(() => { UpdateStatus("Đang tạm dừng 3 giây để chuẩn bị chia lại luồng...", "Yellow"); });
                                    try { await Task.Delay(3000, ct); } catch { }
                                    _isReSegmenting = false;
                                    lastNumConnectionsForUI = -1; // Force UI reset
                                }

                                // Setup UI for Connections (now representing Regions)
                                long regionSize = fileSize / numConnections;
                                if (numConnections != lastNumConnectionsForUI)
                                {
                                    regionDownloaded = new long[numConnections];
                                    lastNumConnectionsForUI = numConnections;
                                    await Dispatcher.InvokeAsync(() =>
                                    {
                                        ConnectionTraceGrid.Children.Clear();
                                        UpdateConnectionTraceOrientation();
                                        workers.Clear();
                                        for (int i = 0; i < numConnections; i++)
                                        {
                                            var pb = new ProgressBar { Minimum = 0, Maximum = 100, Value = 0, Style = (Style)FindResource("RoundedProgressBarStyle"), Margin = new Thickness(1, 0, 1, 0) };
                                            workers.Add(new WorkerData { Index = i, ProgressBar = pb });
                                            ConnectionTraceGrid.Children.Add(pb);
                                        }
                                    });
                                }

                                var workerTasks = new List<Task>();
                                int activeWorkers = 0;
                                Exception fatalError = null;
                                int chunksProcessedInRound = 0;

                                using (var sessionLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _pauseCts.Token))
                                {
                                    for (int i = 0; i < numConnections; i++)
                                    {
                                        int workerIndex = i;
                                        workerTasks.Add(Task.Run(async () =>
                                        {
                                            Interlocked.Increment(ref activeWorkers);
                                            await Dispatcher.InvokeAsync(() => { ConnectionCountTextBlock.Text = $"Threads: {activeWorkers}/{numConnections}"; });
                                            
                                            try
                                            {
                                                var handler = new HttpClientHandler { AllowAutoRedirect = false }; 
                                                using (var chunkClient = new HttpClient(handler))
                                                using (var fs = new FileStream(destinationPath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite | System.IO.FileShare.Delete, 262144, true))
                                                {
                                                    chunkClient.Timeout = TimeSpan.FromMinutes(60);
                                                    chunkClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                                                    while (chunkQueue.TryDequeue(out var range))
                                                    {
                                                        if (sessionLinkedCts.Token.IsCancellationRequested)
                                                        {
                                                            lock (lockObj) { _remainingRanges.Add(range); }
                                                            break;
                                                        }

                                                        try
                                                        {
                                                            string currentChunkUrl = finalUrl;
                                                            HttpResponseMessage response = null;
                                                            
                                                            // Tự động follow redirects mà VẪN giữ được Range header (quan trọng cho tốc độ tải!)
                                                            for (int rd = 0; rd < 10; rd++)
                                                            {
                                                                var request = new HttpRequestMessage(HttpMethod.Get, currentChunkUrl);
                                                                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(range.Start, range.End);
                                                                response = await chunkClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, sessionLinkedCts.Token);

                                                                int statusCode = (int)response.StatusCode;
                                                                if (statusCode >= 300 && statusCode <= 399 && response.Headers.Location != null)
                                                                {
                                                                    currentChunkUrl = response.Headers.Location.IsAbsoluteUri ? response.Headers.Location.AbsoluteUri : new Uri(new Uri(currentChunkUrl), response.Headers.Location).AbsoluteUri;
                                                                    response.Dispose();
                                                                }
                                                                else break;
                                                            }

                                                            using (response)
                                                            {
                                                                response.EnsureSuccessStatusCode();
                                                                using (var stream = await response.Content.ReadAsStreamAsync())
                                                                {
                                                                    fs.Seek(range.Start, SeekOrigin.Begin);
                                                                    byte[] buffer = new byte[256 * 1024]; // 256KB buffer
                                                                    int read;
                                                                    int consecutiveEmptyReads = 0;
                                                                    const int maxConsecutiveEmptyReads = 3;

                                                                    while (consecutiveEmptyReads < maxConsecutiveEmptyReads)
                                                                    {
                                                                        read = await stream.ReadAsync(buffer, 0, buffer.Length, sessionLinkedCts.Token);
                                                                        if (read > 0)
                                                                        {
                                                                            consecutiveEmptyReads = 0; // Reset on successful read
                                                                            await fs.WriteAsync(buffer, 0, read, sessionLinkedCts.Token);
                                                                            range.Downloaded += read;

                                                                            lock (lockObj) { totalDownloaded += read; }

                                                                            long currentPos = range.Start + range.Downloaded - read;
                                                                            int rIdx = (int)Math.Min(currentPos / regionSize, numConnections - 1);
                                                                            lock (regionLock) { regionDownloaded[rIdx] += read; }
                                                                        }
                                                                        else
                                                                        {
                                                                            consecutiveEmptyReads++;
                                                                            if (consecutiveEmptyReads >= maxConsecutiveEmptyReads && range.Downloaded < range.Length)
                                                                            {
                                                                                // Connection stalled before completing chunk, requeue remaining range
                                                                                var remainingRange = new DownloadRange 
                                                                                { 
                                                                                    Start = range.Start + range.Downloaded, 
                                                                                    End = range.End,
                                                                                    Downloaded = 0
                                                                                };
                                                                                lock (lockObj) { _remainingRanges.Add(remainingRange); }
                                                                                throw new Exception("Connection stalled after empty reads");
                                                                            }
                                                                        }
                                                                    }
                                                                    
                                                                    Interlocked.Increment(ref chunksProcessedInRound);

                                                                    // Update final percentage for the region after chunk finish
                                                                    int finalRIdx = (int)Math.Min(range.Start / regionSize, numConnections - 1);
                                                                    long finalRDownloaded;
                                                                    lock (regionLock) { finalRDownloaded = regionDownloaded[finalRIdx]; }
                                                                    long frStart = finalRIdx * regionSize;
                                                                    long frEnd = (finalRIdx == numConnections - 1) ? fileSize - 1 : (frStart + regionSize - 1);
                                                                    int finalRPct = (int)(finalRDownloaded * 100 / (frEnd - frStart + 1));
                                                                    await Dispatcher.InvokeAsync(() => {
                                                                        if (finalRIdx < workers.Count && workers[finalRIdx].ProgressBar != null)
                                                                            workers[finalRIdx].ProgressBar.Value = Math.Min(100, finalRPct);
                                                                    });
                                                                }
                                                            }
                                                        }
                                                        catch (Exception)
                                                        {
                                                            lock (lockObj) { _remainingRanges.Add(range); }
                                                            throw; 
                                                        }
                                                    }
                                                }
                                            }
                                            catch (OperationCanceledException) { }
                                            catch (Exception ex) 
                                            { 
                                                UpdateStatus($"Lỗi luồng {workerIndex}: {ex.Message}", "Red");
                                                sessionLinkedCts.Cancel(); 
                                                lock(lockObj) { fatalError = fatalError ?? ex; }
                                            }
                                            finally
                                            {
                                                Interlocked.Decrement(ref activeWorkers);
                                                await Dispatcher.InvokeAsync(() => { ConnectionCountTextBlock.Text = $"Threads: {activeWorkers}/{numConnections}"; });
                                            }
                                        }));
                                    }
                                    await Task.WhenAll(workerTasks);
                                }

                                if (fatalError != null)
                                    throw fatalError;

                                lock (lockObj)
                                {
                                    if (_remainingRanges.Count > 0)
                                    {
                                        if (chunksProcessedInRound == 0)
                                            throw new Exception("Quá trình kết nối bị từ chối liên tục hoặc file bị khóa. Hủy quá trình.");
                                        foreach (var r in _remainingRanges) chunkQueue.Enqueue(r);
                                        _remainingRanges.Clear();
                                    }
                                }

                                if (totalDownloaded >= fileSize) break;
                            }
                        }
                        finally
                        {
                            uiUpdateCts.Cancel();
                            await uiUpdateTask;
                            uiUpdateCts.Dispose();
                        }

                        // Success
                        await Dispatcher.InvokeAsync(() =>
                        {
                            DownloadProgressBar.Value = 0;
                            DownloadProgressBar.Visibility = Visibility.Visible;
                            ConnectionTraceGrid.Children.Clear();
                            ProgressTextBlock.Text = "";
                            SpeedTextBlock.Text = "";
                        });
                        return;
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries) throw;
                    UpdateStatus($"Lỗi tải (lần {retryCount}): {ex.Message}. Thử lại sau...", "Yellow");
                    await Task.Delay(2000 * retryCount, ct);
                }
            }
        }
        finally
        {
            IsDownloading = false;
        }
    }



        // Fallback: 1 connection, stream thẳng vào file xác
        private async Task DownloadSingleConnectionAsync(string downloadUrl, string destinationPath, string displayName)
        {
            var ct = _cancellationTokenSource?.Token ?? CancellationToken.None;
            int maxRetries = 10;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromMinutes(60);
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
                        DateTime downloadStart = DateTime.Now;
                        DateTime lastUpdate = DateTime.Now;
                        long totalBytes = 0;
                        
                        long lastTotalForSpeed = 0;
                        DateTime lastSpeedUpdate = DateTime.Now;
                        double smoothedSpeed = 0.0; // EMA smoothed speed
                        const double emaAlpha = 0.25; // 25% new, 75% history
                        string speedText = "0 B/s";

                        using (var sessionLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, _pauseCts.Token))
                        using (var response = await client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, sessionLinkedCts.Token))
                        {
                            response.EnsureSuccessStatusCode();
                            long contentLength = response.Content.Headers.ContentLength ?? 0;

                            // ── Bước 1: Tạo file xác với đúng dung lượng thật ──
                            if (contentLength > 0)
                            {
                                using (var placeholder = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                    placeholder.SetLength(contentLength);
                            }

                            using (var stream = await response.Content.ReadAsStreamAsync())
                            // ── Bước 2 & 3: Mở file xác, ghi chunk-by-chunk từ offset 0, không lưu RAM ──
                            using (var fs = new FileStream(destinationPath,
                                contentLength > 0 ? FileMode.Open : FileMode.Create,
                                FileAccess.Write, FileShare.None, 81920, useAsync: true))
                            {
                                fs.Seek(0, SeekOrigin.Begin);
                                int bufferSize = contentLength > 100 * 1024 * 1024 ? 262144 : 81920;
                                byte[] buffer = new byte[bufferSize];
                                int bytesRead;

                                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, sessionLinkedCts.Token)) > 0)
                                {
                                    // Wait for pause event (Asynchronous wait to avoid UI freeze)
                                    await Task.Run(() => _pauseEvent.Wait(ct)); // Check for global stop
                                    sessionLinkedCts.Token.ThrowIfCancellationRequested(); // Check for pause
                                    
                                    // Đắp dữ liệu vào đúng vị trí trong file xác, không lưu RAM
                                    await fs.WriteAsync(buffer, 0, bytesRead, sessionLinkedCts.Token);
                                    totalBytes += bytesRead;

                                    var now = DateTime.Now;
                                    
                                    if ((now - lastSpeedUpdate).TotalMilliseconds >= 250)
                                    {
                                        double rawSpeed = (totalBytes - lastTotalForSpeed) / (now - lastSpeedUpdate).TotalSeconds;
                                        smoothedSpeed = smoothedSpeed == 0.0 ? rawSpeed : emaAlpha * rawSpeed + (1.0 - emaAlpha) * smoothedSpeed;
                                        speedText = FormatSpeed(smoothedSpeed);
                                        lastTotalForSpeed = totalBytes;
                                        lastSpeedUpdate = now;
                                    }

                                    if ((now - lastUpdate).TotalMilliseconds >= 200)
                                    {
                                        int percentage = contentLength > 0 ? (int)((totalBytes * 100L) / contentLength) : 0;
                                        string localSpeedText = speedText;
                                        string capDownloaded = FormatBytes(totalBytes);
                                        string capTotal = (contentLength > 0) ? FormatBytes(contentLength) : "Unknown";

                                        await Dispatcher.InvokeAsync(() =>
                                        {
                                            if (!ct.IsCancellationRequested)
                                            {
                                                DownloadProgressBar.Visibility = Visibility.Visible;
                                                DownloadProgressBar.Value = percentage;
                                                SpeedTextBlock.Text = localSpeedText;
                                                ProgressTextBlock.Text = $"{capDownloaded} / {capTotal}";
                                            }
                                        });

                                        lastUpdate = now;
                                    }
                                }
                            }
                        }

                        await Dispatcher.InvokeAsync(() =>
                        {
                            DownloadProgressBar.Value = 0;
                            DownloadProgressBar.Visibility = Visibility.Visible;
                            ProgressTextBlock.Text = "";
                            SpeedTextBlock.Text = "";
                        });

                        return; // Download thành công
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    
                    // Xóa file xác nếu download thất bại
                    if (File.Exists(destinationPath))
                    {
                        try { File.Delete(destinationPath); } catch { }
                    }

                    if (retryCount >= maxRetries)
                    {
                        UpdateStatus($"❌ Tải file {displayName} thất bại sau {maxRetries} lần thử: {ex.Message}", "Red");
                        throw;
                    }

                    // Exponential backoff
                    int delayMs = 1000 * (int)Math.Pow(2, retryCount);
                    UpdateStatus($"⚠️ Lỗi tải {displayName} (lần {retryCount}/{maxRetries}): {ex.Message}. Thử lại trong {delayMs/1000}s...", "Yellow");

                    if (_cancellationTokenSource != null)
                    {
                        await Task.Delay(delayMs, _cancellationTokenSource.Token);
                    }
                    else
                    {
                        await Task.Delay(delayMs, CancellationToken.None);
                    }
                }
            }
        }

        private void SetupInitialOrientation()
        {
            // Initial State: On app start, detect if screen is Portrait or Landscape
            bool isPortrait = SystemParameters.PrimaryScreenWidth < SystemParameters.PrimaryScreenHeight;

            // Set initial dimensions
            originalWidth = Math.Max(SystemParameters.PrimaryScreenWidth, SystemParameters.PrimaryScreenHeight);
            originalHeight = 18; // Default bar thickness

            UpdateConnectionTraceOrientation();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Reactive Update: Track orientation changes via size changes
            UpdateConnectionTraceOrientation();
        }

        private void UpdateConnectionTraceOrientation()
        {
            if (ConnectionTraceBorder == null || ConnectionTraceGrid == null) return;

            // Detect if orientation is Portrait (Height > Width)
            bool isPortrait = this.ActualHeight > this.ActualWidth;

            if (isPortrait)
            {
                ConnectionTraceBorder.Width = double.NaN;
                ConnectionTraceBorder.Height = originalHeight;
                
                // Adjust UniformGrid for Grid stacking
                ConnectionTraceGrid.Rows = 1; // Single row
                ConnectionTraceGrid.Columns = 0; // Dynamic
            }
            else
            {
                ConnectionTraceBorder.Width = double.NaN;
                ConnectionTraceBorder.Height = originalHeight;

                // Adjust UniformGrid for Horizontal stacking
                ConnectionTraceGrid.Rows = 1; // Single row
                ConnectionTraceGrid.Columns = 0; // Dynamic
            }
        }

        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond > 1024 * 1024)
            {
                return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
            }
            else if (bytesPerSecond > 1024)
            {
                return $"{bytesPerSecond / 1024:F2} KB/s";
            }
            else
            {
                return $"{bytesPerSecond:F2} B/s";
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes > 1024 * 1024 * 1024)
            {
                return $"{(double)bytes / (1024 * 1024 * 1024):F2} GB";
            }
            else if (bytes > 1024 * 1024)
            {
                return $"{(double)bytes / (1024 * 1024):F2} MB";
            }
            else if (bytes > 1024)
            {
                return $"{(double)bytes / 1024:F2} KB";
            }
            else
            {
                return $"{bytes} B";
            }
        }

        private void CboSegmentCount_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            if (CboSegmentCount.IsMouseOver)
            {
                if (e.Delta > 0 && CboSegmentCount.SelectedIndex > 0)
                {
                    CboSegmentCount.SelectedIndex--;
                }
                else if (e.Delta < 0 && CboSegmentCount.SelectedIndex < CboSegmentCount.Items.Count - 1)
                {
                    CboSegmentCount.SelectedIndex++;
                }
                e.Handled = true;
            }
        }

        private void CboSegmentCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboSegmentCount?.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                if (int.TryParse(item.Content.ToString(), out int newCount))
                {
                    _isReSegmenting = true; // Luôn đánh dấu để khi Resume (hoặc Auto-restart) sẽ có delay 3s

                    if (_pauseEvent != null && _pauseEvent.IsSet)
                    {
                        UpdateStatus($"Đang điều chỉnh số luồng tải thành {newCount} và khởi động lại phiên tải...", "Cyan");
                        _pauseCts?.Cancel();
                    }
                    else
                    {
                        UpdateStatus($"Đã thay đổi số luồng tải thành {newCount}. Bạn hãy nhấn Resume để áp dụng sau 3 giây chờ.", "Cyan");
                    }
                }
            }
        }
    }
}
