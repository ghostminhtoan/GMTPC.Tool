// =======================================================================
// MainWindow.SystemDownload.cs
// =======================================================================
// CHỨA DUY NHẤT: Logic tải file (Download Engine)
// KHÔNG chứa: Install logic, Process start, MessageBox, UI event handlers
//
// Kiến trúc tải file chuẩn hóa cho toàn bộ ứng dụng
// Tất cả checkbox phải gọi qua các hàm trong file này
// 
// Cập nhật: 2026-03-14 - Hỗ trợ pause 2 giây khi đổi segment, gộp chunks
// =======================================================================
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GMTPC.Tool.Services;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
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
        // Dùng 16 segments để tăng tốc độ tải
        // ===================================================================
        private async Task DownloadSingleLinkFastAsync(string downloadUrl, string destinationPath, string displayName)
        {
            await _downloadSemaphore.WaitAsync();
            try
            {
                var ct = _cancellationTokenSource?.Token ?? CancellationToken.None;

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

                UpdateStatus($"Đang tải {displayName}...", "Cyan");

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
                    // FAST PATH + MULTI-SEGMENT: Skip probe, download with 16 segments
                    var engine = new SegmentedDownloadEngineOptimized();
                    await engine.DownloadMultiSegmentFastAsync(downloadUrl, destinationPath, 16, uiProgress, ct, _pauseEvent);

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
