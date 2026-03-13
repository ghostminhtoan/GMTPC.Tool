// =======================================================================
// MainWindow.SystemDownload.cs
// =======================================================================
// CHỨA DUY NHẤT: Logic tải file (Download Engine)
// KHÔNG chứa: Install logic, Process start, MessageBox, UI event handlers
//
// Kiến trúc tải file chuẩn hóa cho toàn bộ ứng dụng
// Tất cả checkbox phải gọi qua các hàm trong file này
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
        // FAST SINGLE-LINK DOWNLOAD - "Golden Standard"
        // ===================================================================
        // Sử dụng cho: TẤT CẢ checkbox tải đơn link (1 file .exe, .msi)
        // Ví dụ: VPN1111, MMT Apps, Google Drive, DISM++, NetLimiter, etc.
        //
        // Tại sao nhanh? Bỏ qua ProbeAsync (HEAD request) → tải ngay lập tức
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

                UpdateStatus($"Dang tai {displayName}...", "Cyan");

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
                    // FAST PATH: Skip probe, download immediately
                    var engine = new SegmentedDownloadEngineOptimized();
                    await engine.DownloadSingleFastAsync(downloadUrl, destinationPath, uiProgress, ct, _pauseEvent);

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
                UpdateStatus($"Loi tai: {ex.Message}", "Red");
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
                    var engine = new SegmentedDownloadEngineOptimized();
                    await engine.DownloadAsync(downloadUrl, destinationPath, segments, uiProgress, ct, _pauseEvent);

                    await Dispatcher.InvokeAsync(() => ResetDownloadUI());
                }
                finally
                {
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
        // Mỗi part được tải tuần tự, tất cả vào cùng 1 thư mục
        // ===================================================================
        private async Task DownloadMultiPartAsync(
            string[] downloadUrls, 
            string[] destinationPaths, 
            string displayNamePrefix)
        {
            for (int i = 0; i < downloadUrls.Length; i++)
            {
                string partName = $"{displayNamePrefix} - Part {i + 1}/{downloadUrls.Length}";
                UpdateStatus($"Dang tai {partName}...", "Cyan");
                
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

        // ===================================================================
        // HELPER: Format Speed (B/s, KB/s, MB/s, GB/s)
        // ===================================================================
        private string FormatSpeed(double bytesPerSecond)
        {
            if (bytesPerSecond < 1024)
                return $"{bytesPerSecond:F2} B/s";
            if (bytesPerSecond < 1024 * 1024)
                return $"{bytesPerSecond / 1024:F2} KB/s";
            if (bytesPerSecond < 1024 * 1024 * 1024)
                return $"{bytesPerSecond / (1024 * 1024):F2} MB/s";
            return $"{bytesPerSecond / (1024 * 1024 * 1024):F2} GB/s";
        }

        // ===================================================================
        // HELPER: Format Bytes (B, KB, MB, GB, TB)
        // ===================================================================
        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}
