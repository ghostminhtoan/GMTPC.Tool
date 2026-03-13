// =======================================================================
// MainWindow.TabWindows.cs
// Chức năng: Xử lý checkbox và cài đặt cho Tab Windows - Microsoft
// Cập nhật: 2026-03-10 - Xóa Win 10 20H2 2022 April - onedrive
// =======================================================================
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // ===================================================================
        // TabWindows — Checkbox Click Handlers
        // TabItem Header: "Windows - Microsoft"
        // Checkboxes: ChkWin11_26H1
        // ===================================================================
        private void ChkWin11_26H1_Click(object sender, RoutedEventArgs e)
        {
            if (ChkWin11_26H1.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Win 11 - 26H1 - 2026 Feb - server archive.org", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Win 11 - 26H1 - 2026 Feb - server archive.org", "Yellow");
            }

            UpdateInstallButtonState();
        }

        // ===================================================================
        // TabWindows — Install Methods
        // ===================================================================
        private async Task InstallWin11_26H1Async()
        {
            try
            {
                UpdateStatus("Đang tải Win 11 - 26H1 - 2026 Feb...", "Cyan");
                string win11Path = Path.Combine(GetGMTPCFolder(), "Win11_26H1.iso");
                await DownloadWithProgressAsync("https://archive.org/download/microsoft-win11-26h2-february-2026/en-us_windows_11_consumer_editions_version_26h1_x64_dvd_5208fe5b.iso", win11Path, "Win 11 26H1");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Tải Win 11 26H1 hoàn tất! File ISO đã được lưu tại: " + win11Path, "Green");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải Win 11 26H1: {ex.Message}", "Red");
            }
        }
    }
}
