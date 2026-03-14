// =======================================================================
// MainWindow.SystemBar.cs
// AI Summary:
// Date: 2026-03-14
// - Fixed CboSegmentCount_SelectionChanged: Now pause for 2 seconds before resume
// - Allows proper segment change during download without stopping
// Chức năng: Xử lý progress bar, connection trace, và download UI
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
using GMTPC.Tool.Services;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // Fields
        private double originalWidth;
        private double originalHeight;
        private bool _isReSegmenting;
        private CancellationTokenSource _pauseCts;
        private ConcurrentQueue<DownloadRange> _remainingRanges = new ConcurrentQueue<DownloadRange>();

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

        public bool IsDownloading { get; private set; }

        private void UpdateStatus(string message, string color)
        {
            // Placeholder - StatusTextBlock not available in this version
            System.Diagnostics.Debug.WriteLine($"[Status] {message}");
        }

        private void UpdateSecondaryStatus(string message, string color)
        {
            // Placeholder for secondary status
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
            // Chỉ xử lý đổi segment khi đang tải
            if (!IsDownloading || _pauseEvent == null)
                return;

            if (CboSegmentCount?.SelectedItem is ComboBoxItem item && item.Content != null)
            {
                if (int.TryParse(item.Content.ToString(), out int newCount))
                {
                    UpdateStatus($"Đã chọn {newCount} luồng. Đang tạm dừng 2 giây để thay đổi luồng download...", "Yellow");

                    // Bước 1: Tạm dừng quá trình tải (PAUSE không CANCEL)
                    DownloadRegistry.PauseAll();
                    BtnPause.Content = "Resume";

                    // Bước 2: Sau 2 giây, RESUME để tiếp tục với số segment mới
                    // ENGINE sẽ đọc CboSegmentCount.SelectedItem để lấy số segment mới
                    Dispatcher.InvokeAsync(async () =>
                    {
                        await Task.Delay(2000); // Chờ 2 giây để pause hoàn toàn

                        // RESUME: Download engine sẽ tiếp tục với số segment mới
                        // vì nó được đọc từ CboSegmentCount mỗi khi bắt đầu chunk mới
                        DownloadRegistry.ResumeAll();
                        BtnPause.Content = "Pause";
                        UpdateStatus($"Đang tiếp tục tải với {newCount} luồng...", "Cyan");
                    });
                }
            }
        }
    }
}
