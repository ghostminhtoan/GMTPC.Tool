// =======================================================================
// MainWindow.SystemInstallDefault.cs
// Chức năng: Cơ chế cài đặt cơ bản - tải file và chạy với argument
// Cập nhật gần đây:
//   - 2026-03-17: Tạo mới theo yêu cầu phân loại cơ chế cài đặt
// =======================================================================
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // ===================== SystemInstallDefault - Cài đặt cơ bản =====================
        /// <summary>
        /// Cơ chế cài đặt cơ bản: Tải file và chạy với argument
        /// Sử dụng cho các checkbox cài đặt phần mềm thông thường
        /// </summary>
        /// <param name="downloadUrl">Link tải về</param>
        /// <param name="filePath">Đường dẫn lưu file</param>
        /// <param name="installArguments">Tham số cài đặt (ví dụ: /s, /silent, /quiet)</param>
        /// <param name="displayName">Tên hiển thị (ví dụ: "IDM", "WinRAR")</param>
        protected async Task InstallWithDefaultAsync(string downloadUrl, string filePath, string installArguments, string displayName)
        {
            try
            {
                // Step 1: Download file
                UpdateStatus($"Đang tải {displayName}...", "Cyan");
                await DownloadWithProgressAsync(downloadUrl, filePath, displayName);

                // Reset progress bar
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Step 2: Run installer with arguments
                UpdateStatus($"Đang cài đặt {displayName} ( {installArguments} )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    Arguments = installArguments,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus($"Cài đặt {displayName} hoàn tất.", "Green");
                }

                // Step 3: Delete installer
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    UpdateStatus($"Đã xóa file cài đặt {displayName}", "Cyan");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài {displayName}: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Cơ chế cài đặt cơ bản với retry logic
        /// </summary>
        protected async Task InstallWithDefaultAndRetryAsync(string downloadUrl, string filePath, string installArguments, string displayName, int maxRetries = 3)
        {
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    await InstallWithDefaultAsync(downloadUrl, filePath, installArguments, displayName);
                    return; // Success
                }
                catch (Exception ex)
                {
                    retryCount++;
                    if (retryCount >= maxRetries)
                    {
                        UpdateStatus($"Lỗi sau {maxRetries} lần thử: {ex.Message}", "Red");
                        throw;
                    }
                    UpdateStatus($"Thử lại lần {retryCount}/{maxRetries}...", "Yellow");
                    await Task.Delay(2000); // Wait 2 seconds before retry
                }
            }
        }
    }
}
