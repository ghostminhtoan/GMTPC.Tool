using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        private void ChkWin11_26H1_Click(object sender, RoutedEventArgs e)
        {
            UpdateInstallButtonState();
        }

        private void ChkWin10_20H2_2022April_Click(object sender, RoutedEventArgs e)
        {
            UpdateInstallButtonState();
        }

        private async Task InstallWin10_20H2_2022AprilAsync()
        {
            // Link chia sẻ từ OneDrive/SharePoint — cần resolve thành direct binary URL
            string shareUrl = "https://glennsferryschools-my.sharepoint.com/:u:/g/personal/billgates_glennsferryschools_onmicrosoft_com/Ed8HqTyoPFxLktIGaRFqDOYBQP5hWqV8d69Qq9TJ-k9L0A?download=1";
            string fileName = "en-us_windows_10_consumer_editions_version_20h2_updated_april_2022_x64.iso";
            string destinationPath = Path.Combine("C:\\", fileName);

            UpdateStatus("Đang resolve link OneDrive/SharePoint...", "Cyan");

            try
            {
                // Bước 1: Resolve SharePoint share URL → direct binary download URL
                string directUrl = await ResolveOneDriveDirectUrlAsync(shareUrl);

                UpdateStatus("Đang kết nối tới server và bắt đầu tải...", "Cyan");

                // Bước 2: Feed direct URL vào engine 16-segment (tự động fallback 1-thread nếu server không hỗ trợ Range)
                await DownloadWithProgressAsync(directUrl, destinationPath, "Đang tải về ổ C - Win 10 - 20H2 April 2022");

                UpdateStatus("Tải xong! Đang mở ổ C và file ISO...", "Green");
                Process.Start("explorer.exe", "C:\\");
                Process.Start(new ProcessStartInfo { FileName = destinationPath, UseShellExecute = true });
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Đã hủy tải Win 10 20H2.", "Yellow");
                if (File.Exists(destinationPath))
                    try { File.Delete(destinationPath); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi tải Win 10 20H2: {ex.Message}", "Red");
                throw;
            }
        }

        private async Task InstallWin11_26H1Async()
        {
            string url = "https://archive.org/download/microsoft-win11-26h2-february-2026/en-us_windows_11_consumer_editions_version_26h1_x64_dvd_5208fe5b.iso";
            string fileName = "en-us_windows_11_26h1_x64.iso";
            string destinationPath = Path.Combine("C:\\", fileName);

            UpdateStatus("Đang kết nối tới server archive.org...", "Cyan");

            try
            {
                // archive.org hỗ trợ Range requests → engine 16-segment chạy tốt
                await DownloadWithProgressAsync(url, destinationPath, "Đang tải về ổ C - Win 11 - 26H1");

                UpdateStatus("Tải xong! Đang mở ổ C và file ISO...", "Green");
                Process.Start("explorer.exe", "C:\\");
                Process.Start(new ProcessStartInfo { FileName = destinationPath, UseShellExecute = true });
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Đã hủy tải Win 11.", "Yellow");
                if (File.Exists(destinationPath))
                    try { File.Delete(destinationPath); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi tải Win 11: {ex.Message}", "Red");
                throw;
            }
        }
    }
}
