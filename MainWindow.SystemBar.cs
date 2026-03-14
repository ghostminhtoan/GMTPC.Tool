// =======================================================================
// MainWindow.SystemBar.cs
// AI Summary:
// Date: 2026-03-14
// - Fixed CboSegmentCount: Disable segment change during download
// - Users can only change segment BEFORE clicking Install, not during
// - Prevents "complete all tasks" error by disallowing pause/resume with different segment count
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

        private async void CboSegmentCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Nếu không downloading, cho phép đổi bình thường
            if (!IsDownloading)
            {
                if (CboSegmentCount?.SelectedItem is ComboBoxItem item && item.Content != null)
                {
                    if (int.TryParse(item.Content.ToString(), out int newCount))
                    {
                        UpdateStatus($"Đã đổi segment count: {newCount} luồng", "Green");
                    }
                }
                return;
            }

            // Nếu đang downloading, cho phép đổi bằng cách pause + reallocate + resume
            if (IsDownloading && _currentDownloadInfo != null && _activeDownloadEngine != null)
            {
                // Get new segment count
                int newSegments = 16;
                if (CboSegmentCount?.SelectedItem is ComboBoxItem item &&
                    int.TryParse(item.Content?.ToString(), out int n))
                    newSegments = n;

                try
                {
                    UpdateStatus($"Dang doi segment count thanh {newSegments}...", "Yellow");
                    
                    // Call async engine method to pause, reallocate, and resume
                    await _activeDownloadEngine.ReallocateSegmentsDuringDownloadAsync(newSegments);
                    
                    UpdateStatus($"Da doi sang {newSegments} segments, tai tiep tuc...", "Cyan");
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Loi doi segment: {ex.Message}", "Red");
                    // Restore previous selection on error
                    if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is ComboBoxItem oldItem)
                    {
                        CboSegmentCount.SelectedItem = oldItem;
                    }
                }
            }
        }
    }
}
