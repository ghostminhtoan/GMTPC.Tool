// =======================================================================
// MainWindow.SystemBar.cs
// Chức năng: Xử lý Progress Bar, Segment UI, download engine,
//            thông báo trạng thái, shared state fields
// Cập nhật gần đây:
//   - 2026-03-05: Chuyển UpdateStatus, UpdateSecondaryStatus, SetInstallingState
//                 và các shared fields từ xaml.cs về đây theo AI_WORKFLOW.md
//   - 2026-03-07: Thêm hiển thị Build Number theo định dạng YYYY-MM-DD-hh-mm-ss
//   - 2026-03-17: Thêm Windows Defender exclusion management cho temp folder
// =======================================================================
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
using System.Windows.Media;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // ===================== Shared State Fields =====================
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _pauseCts;
        private System.Threading.ManualResetEventSlim _pauseEvent = new System.Threading.ManualResetEventSlim(true);
        private List<DownloadRange> _remainingRanges = new List<DownloadRange>();

        private bool _isInstalling = false;
        private string _installationStatus = "";
        private double originalWidth;
        private double originalHeight;

        // Temp folder selection
        private string _selectedTempDrivePath = null;
        private string _previousTempFolderPath = null; // Track previous temp folder for cleanup
        private string _systemTempFolderPath = null; // System folder path (LocalAppData) - never delete exclusion

        // ===================== Build Number Display =====================
        private void SetBuildNumber()
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (BuildNumberTextBlock != null)
                {
                    BuildNumberTextBlock.Text = $"Build: {BuildInfo.BUILD_NUMBER}";
                }
            });
        }

        // ===================== Status Methods =====================
        private void UpdateStatus(string message, string color)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (_isInstalling)
                {
                    _installationStatus = message;
                    ProgressTextBlock.Text = message;
                    ProgressTextBlock.Foreground = GetBrush(color);
                }
                else
                {
                    ProgressTextBlock.Text = message;
                    ProgressTextBlock.Foreground = GetBrush(color);
                }
            });
        }

        private void UpdateSecondaryStatus(string message, string color = "Gray")
        {
            Dispatcher.InvokeAsync(() =>
            {
                SecondaryProgressTextBlock.Text = message;
                SecondaryProgressTextBlock.Foreground = GetBrush(color);

                if (!_isInstalling)
                {
                    ProgressTextBlock.Text = message;
                    ProgressTextBlock.Foreground = GetBrush(color);
                }
            });
        }

        private void SetInstallingState(bool isInstalling)
        {
            _isInstalling = isInstalling;
            if (!isInstalling)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    SecondaryProgressTextBlock.Text = "";
                });
            }
        }
        private class DownloadRange
        {
            public long Start { get; set; }
            public long End { get; set; }
            public long Downloaded { get; set; }
            public long Length => End - Start + 1;
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
        /// Download đặc biệt cho OneDrive/SharePoint - Dùng 16 threads như DownloadWithProgressAsync
        /// Nhưng với retry logic đơn giản hơn, không stall detection phức tạp
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

                    // Dùng lại DownloadWithProgressAsync nhưng với User-Agent đặc biệt
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromHours(2);
                        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                        client.DefaultRequestHeaders.Add("Accept", "*/*");
                        
                        // Thăm dò để lấy file size
                        var headRequest = new HttpRequestMessage(HttpMethod.Head, downloadUrl);
                        var headResponse = await client.SendAsync(headRequest);
                        long fileSize = headResponse.Content.Headers.ContentLength ?? 0;
                        
                        if (fileSize == 0)
                        {
                            // Nếu không lấy được size, thử GET request với Range header
                            var probeRequest = new HttpRequestMessage(HttpMethod.Get, downloadUrl);
                            probeRequest.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0);
                            var probeResponse = await client.SendAsync(probeRequest, HttpCompletionOption.ResponseHeadersRead);
                            
                            if (probeResponse.StatusCode == System.Net.HttpStatusCode.PartialContent)
                            {
                                fileSize = probeResponse.Content.Headers.ContentRange?.Length ?? 0;
                            }
                        }

                        // Nếu vẫn không có size, tải single-thread
                        if (fileSize == 0)
                        {
                            UpdateStatus("Không lấy được kích thước file, chuyển sang chế độ 1 thread...", "Yellow");
                            await DownloadSingleConnectionAsync(downloadUrl, destinationPath, displayName);
                        }
                        else
                        {
                            // Dùng DownloadWithProgressAsync với 16 threads
                            await DownloadWithProgressAsync(downloadUrl, destinationPath, displayName);
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
                        UpdateStatus("Đang thử lại sau 3 giây...", "Cyan");
                        await Task.Delay(3000);
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

        // -- Download semaphore: serialises downloads, never gets `stuck` ------
        private static readonly SemaphoreSlim _downloadSemaphore = new SemaphoreSlim(1, 1);

        /// <summary>True while a download is in progress.</summary>
        public bool IsDownloading => _downloadSemaphore.CurrentCount == 0;

        /// <summary>
        /// Main download entry-point. Delegates to SegmentedDownloadEngine via IProgress.
        /// </summary>
        private async Task DownloadWithProgressAsync(string downloadUrl, string destinationPath, string displayName = "File")
        {
            await _downloadSemaphore.WaitAsync();
            try
            {
                var ct = _cancellationTokenSource?.Token ?? CancellationToken.None;

                int segments = 8;
                await Dispatcher.InvokeAsync(() =>
                {
                    if (CboSegmentCount?.SelectedItem is ComboBoxItem item &&
                        int.TryParse(item.Content?.ToString(), out int n))
                        segments = n;
                });

                Progress<GMTPC.Tool.Services.DownloadProgressInfo> uiProgress = null;
                await Dispatcher.InvokeAsync(() =>
                {
                    ResetDownloadUI();
                    uiProgress = new Progress<GMTPC.Tool.Services.DownloadProgressInfo>(info =>
                    {
                        if (!ct.IsCancellationRequested)
                            ApplyDownloadProgressToUI(info);
                    });
                });

                UpdateStatus($"Dang tai {displayName}...", "Cyan");

                var engine = new GMTPC.Tool.Services.SegmentedDownloadEngine();
                await engine.DownloadAsync(downloadUrl, destinationPath, segments, uiProgress, ct);

                await Dispatcher.InvokeAsync(() => ResetDownloadUI());
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

        private void ApplyDownloadProgressToUI(GMTPC.Tool.Services.DownloadProgressInfo info)
        {
            bool isSingle = info.SegmentPercents == null || info.SegmentPercents.Length <= 1;
            DownloadProgressBar.Visibility = isSingle ? Visibility.Visible : Visibility.Collapsed;
            if (isSingle && info.SegmentPercents != null && info.SegmentPercents.Length == 1)
                DownloadProgressBar.Value = info.SegmentPercents[0];
            SpeedTextBlock.Text = FormatSpeed(info.SpeedBytesPerSec);
            ProgressTextBlock.Text = info.TotalBytes > 0
                ? $"{FormatBytes(info.BytesDone)} / {FormatBytes(info.TotalBytes)}"
                : FormatBytes(info.BytesDone);
            if (!isSingle && info.SegmentPercents != null)
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
                            Margin = new System.Windows.Thickness(1, 0, 1, 0)
                        });
                }
                for (int s = 0; s < count && s < ConnectionTraceGrid.Children.Count; s++)
                    if (ConnectionTraceGrid.Children[s] is ProgressBar pb)
                        pb.Value = info.SegmentPercents[s];
                ConnectionCountTextBlock.Text = $"Threads: {count}";
            }
        }

        private void ResetDownloadUI()
        {
            DownloadProgressBar.Value = 0;
            DownloadProgressBar.Visibility = Visibility.Visible;
            ConnectionTraceGrid.Children.Clear();
            ProgressTextBlock.Text = "";
            SpeedTextBlock.Text = "";
            ConnectionCountTextBlock.Text = "";
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
        /// <summary>
        /// Follows redirects on a OneDrive/SharePoint share URL to retrieve
        /// the final direct binary download URL.
        /// </summary>
        private async Task<string> ResolveOneDriveDirectUrlAsync(string shareUrl)
        {
            const int maxRedirects = 10;
            string currentUrl = shareUrl;

            using (var handler = new HttpClientHandler { AllowAutoRedirect = false })
            using (var client  = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) })
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");

                for (int i = 0; i < maxRedirects; i++)
                {
                    var request = new HttpRequestMessage(HttpMethod.Head, currentUrl);
                    var response = await client.SendAsync(request,
                        HttpCompletionOption.ResponseHeadersRead);

                    int code = (int)response.StatusCode;
                    if (code >= 300 && code <= 399 && response.Headers.Location != null)
                    {
                        Uri location = response.Headers.Location;
                        currentUrl = location.IsAbsoluteUri
                            ? location.AbsoluteUri
                            : new Uri(new Uri(currentUrl), location).AbsoluteUri;
                        continue;
                    }

                    // If success or non-redirect, this is our direct URL
                    if (response.IsSuccessStatusCode)
                        return currentUrl;

                    throw new Exception($"Không thể resolve URL OneDrive. HTTP {code}: {currentUrl}");
                }
            }

            throw new Exception("Quá nhiều lần redirect khi resolve URL OneDrive.");
        }

        // ===================== Temp Folder ComboBox =====================
        /// <summary>
        /// Populate the Temp folder ComboBox with all available drives (excluding CD-ROM)
        /// </summary>
        private void PopulateTempFolderComboBox()
        {
            try
            {
                if (CboTempFolder == null) return;

                CboTempFolder.Items.Clear();

                // Add default option (C:\Temp Folder)
                string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GMTPC", "GMTPC Tools");
                CboTempFolder.Items.Add(new ComboBoxItem
                {
                    Content = $"Mặc định (C:) - {defaultPath}",
                    Tag = defaultPath
                });

                // Add all drives except CD-ROM
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.DriveType != DriveType.CDRom && drive.IsReady)
                    {
                        string drivePath = Path.Combine(drive.Name.TrimEnd('\\'), "Temp Folder");
                        string displayText = $"{drive.Name} ({FormatBytes(drive.TotalFreeSpace)} free)";

                        CboTempFolder.Items.Add(new ComboBoxItem
                        {
                            Content = displayText,
                            Tag = drivePath
                        });
                    }
                }

                // Select default (first) item
                if (CboTempFolder.Items.Count > 0)
                {
                    CboTempFolder.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải danh sách ổ cứng: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Handle Temp folder ComboBox selection changed
        /// Auto-create folder, delete old temp folder (except system folder)
        /// and manage Windows Defender exclusions
        /// </summary>
        private async void CboTempFolder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (CboTempFolder.SelectedItem is ComboBoxItem selectedItem)
                {
                    string newTempPath = selectedItem.Tag as string;

                    if (!string.IsNullOrEmpty(newTempPath))
                    {
                        // Initialize system temp folder path on first run
                        if (_systemTempFolderPath == null)
                        {
                            _systemTempFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GMTPC", "GMTPC Tools");
                            
                            // Always ensure system folder has defender exclusion (never remove it)
                            await AddDefenderExclusionAsync(_systemTempFolderPath);
                        }

                        // Remove defender exclusion from previous temp folder if it's not the system folder
                        if (!string.IsNullOrEmpty(_previousTempFolderPath) &&
                            _previousTempFolderPath != newTempPath &&
                            !_previousTempFolderPath.Equals(_systemTempFolderPath, StringComparison.OrdinalIgnoreCase))
                        {
                            await RemoveDefenderExclusionAsync(_previousTempFolderPath);

                            // Delete previous temp folder if it's not the system folder
                            try
                            {
                                if (Directory.Exists(_previousTempFolderPath))
                                {
                                    Directory.Delete(_previousTempFolderPath, true);
                                    UpdateStatus($"Đã xóa folder tạm: {_previousTempFolderPath}", "Gray");
                                }
                            }
                            catch (Exception ex)
                            {
                                UpdateStatus($"Không thể xóa folder cũ ({_previousTempFolderPath}): {ex.Message}", "Yellow");
                            }
                        }

                        // Create new temp folder
                        if (!Directory.Exists(newTempPath))
                        {
                            try
                            {
                                Directory.CreateDirectory(newTempPath);
                                UpdateStatus($"Đã tạo folder: {newTempPath}", "Green");
                            }
                            catch (Exception ex)
                            {
                                UpdateStatus($"Không thể tạo folder ({newTempPath}): {ex.Message}", "Red");
                                return;
                            }
                        }

                        // Add defender exclusion for new temp folder (skip if it's the system folder - already added)
                        if (!newTempPath.Equals(_systemTempFolderPath, StringComparison.OrdinalIgnoreCase))
                        {
                            await AddDefenderExclusionAsync(newTempPath);
                        }

                        // Update status and track previous path
                        _previousTempFolderPath = newTempPath;
                        _selectedTempDrivePath = newTempPath;
                        UpdateStatus($"Temp folder: {newTempPath}", "Green");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi chọn folder: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Get the selected temp folder path
        /// </summary>
        private string GetSelectedTempFolderPath()
        {
            if (!string.IsNullOrEmpty(_selectedTempDrivePath))
            {
                if (!Directory.Exists(_selectedTempDrivePath))
                {
                    Directory.CreateDirectory(_selectedTempDrivePath);
                }
                return _selectedTempDrivePath;
            }

            // Default to LocalAppData if nothing selected
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GMTPC", "GMTPC Tools");
        }

        // ===================== Defender Exclusion Methods =====================
        /// <summary>
        /// Add Windows Defender exclusion for a folder path
        /// Requires administrator privileges
        /// </summary>
        private async Task AddDefenderExclusionAsync(string folderPath)
        {
            try
            {
                // Check if running as administrator
                if (!IsRunningAsAdministrator())
                {
                    UpdateStatus($"Không thể thêm Defender exclusion (cần admin): {folderPath}", "Yellow");
                    return;
                }

                // Check if exclusion already exists
                if (await IsDefenderExclusionExistsAsync(folderPath))
                {
                    UpdateStatus($"Defender exclusion đã tồn tại: {folderPath}", "Gray");
                    return;
                }

                // Use PowerShell to add exclusion
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Add-MpPreference -ExclusionPath '{folderPath}' -Force\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = Process.Start(startInfo))
                {
                    await Task.Run(() =>
                    {
                        process.WaitForExit(10000); // Wait up to 10 seconds
                    });

                    if (process.ExitCode == 0)
                    {
                        UpdateStatus($"Đã thêm Defender exclusion: {folderPath}", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Không thể thêm Defender exclusion (exit code: {process.ExitCode}): {folderPath}", "Yellow");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi thêm Defender exclusion: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Remove Windows Defender exclusion for a folder path
        /// Requires administrator privileges
        /// </summary>
        private async Task RemoveDefenderExclusionAsync(string folderPath)
        {
            try
            {
                // Check if running as administrator
                if (!IsRunningAsAdministrator())
                {
                    UpdateStatus($"Không thể xóa Defender exclusion (cần admin): {folderPath}", "Yellow");
                    return;
                }

                // Check if exclusion exists
                if (!await IsDefenderExclusionExistsAsync(folderPath))
                {
                    return; // Exclusion doesn't exist, nothing to remove
                }

                // Use PowerShell to remove exclusion
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Remove-MpPreference -ExclusionPath '{folderPath}' -Force\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = Process.Start(startInfo))
                {
                    await Task.Run(() =>
                    {
                        process.WaitForExit(10000); // Wait up to 10 seconds
                    });

                    if (process.ExitCode == 0)
                    {
                        UpdateStatus($"Đã xóa Defender exclusion: {folderPath}", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Không thể xóa Defender exclusion (exit code: {process.ExitCode}): {folderPath}", "Yellow");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi xóa Defender exclusion: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Check if a folder path is already in Windows Defender exclusion list
        /// </summary>
        private async Task<bool> IsDefenderExclusionExistsAsync(string folderPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = "-Command \"Get-MpPreference | Select-Object -ExpandProperty ExclusionPath\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardOutput = true
                };

                using (var process = Process.Start(startInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    process.WaitForExit(5000);

                    if (!string.IsNullOrEmpty(output))
                    {
                        var exclusions = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var exclusion in exclusions)
                        {
                            if (exclusion.Trim().Equals(folderPath.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                return true;
                            }
                        }
                    }

                    return false;
                }
            }
            catch
            {
                return false; // Assume not exists if check fails
            }
        }

        /// <summary>
        /// Check if running as administrator
        /// </summary>
        private bool IsRunningAsAdministrator()
        {
            try
            {
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

    }
}
