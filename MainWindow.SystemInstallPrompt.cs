// =======================================================================
// MainWindow.SystemInstallPrompt.cs
// Chức năng: Cơ chế cài đặt có hộp thoại Yes/No - chọn cài tự động hoặc mở bình thường
// Cập nhật gần đây:
//   - 2026-03-17: Tạo mới theo yêu cầu phân loại cơ chế cài đặt (giống MMT Apps)
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
        // ===================== SystemInstallPrompt - Cài đặt có hộp thoại Yes/No =====================
        /// <summary>
        /// Cơ chế cài đặt có hộp thoại Yes/No
        /// Sau khi tải xong, hỏi người dùng có muốn cài tự động không
        /// Chọn Yes: chạy với lệnh /s để cài tự động
        /// Chọn No: mở file bình thường
        /// </summary>
        /// <param name="downloadUrl">Link tải về</param>
        /// <param name="filePath">Đường dẫn lưu file</param>
        /// <param name="silentArguments">Tham số cài tự động (ví dụ: /s, /silent)</param>
        /// <param name="displayName">Tên hiển thị (ví dụ: "MMT Apps", "Office Tool")</param>
        protected async Task InstallWithPromptAsync(string downloadUrl, string filePath, string silentArguments, string displayName)
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

                // Step 2: Show Yes/No dialog
                UpdateStatus($"Đang chờ người dùng chọn cách cài đặt {displayName}...", "Yellow");
                
                bool installSilently = false;
                Dispatcher.Invoke(() =>
                {
                    MessageBoxResult result = MessageBox.Show(
                        $"Bạn có muốn cài đặt {displayName} tự động không?\n\n" +
                        $"Yes: Cài tự động (silent mode)\n" +
                        $"No: Mở file để cài thủ công",
                        $"Cài đặt {displayName}",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    installSilently = (result == MessageBoxResult.Yes);
                });

                // Step 3: Run installer based on user choice
                if (installSilently)
                {
                    // User chose Yes - install silently
                    UpdateStatus($"Đang cài đặt {displayName} tự động ( {silentArguments} )...", "Yellow");
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        Arguments = silentArguments,
                        UseShellExecute = true
                    };
                    Process process = Process.Start(startInfo);
                    if (process != null)
                    {
                        await Task.Run(() => process.WaitForExit());
                        UpdateStatus($"Cài đặt {displayName} tự động hoàn tất.", "Green");
                    }
                }
                else
                {
                    // User chose No - open file normally
                    UpdateStatus($"Đang mở {displayName}...", "Yellow");
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                    UpdateStatus($"Đã mở {displayName}. Vui lòng cài thủ công.", "Green");
                }

                // Step 4: Delete installer (only if installed silently)
                if (installSilently && File.Exists(filePath))
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
        /// Cơ chế cài đặt có hộp thoại Yes/No với retry logic
        /// </summary>
        protected async Task InstallWithPromptAndRetryAsync(string downloadUrl, string filePath, string silentArguments, string displayName, int maxRetries = 3)
        {
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    await InstallWithPromptAsync(downloadUrl, filePath, silentArguments, displayName);
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
