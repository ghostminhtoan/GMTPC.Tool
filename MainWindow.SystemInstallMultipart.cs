// =======================================================================
// MainWindow.SystemInstallMultipart.cs
// Chức năng: Cơ chế cài đặt Multi-part - tải nhiều split file, tự động ghép và xóa
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
        // ===================== SystemInstallMultipart - Cài đặt Multi-part =====================
        /// <summary>
        /// Cơ chế cài đặt Multi-part: Tải nhiều split file (.001, .002, .003...)
        /// Sau khi tải xong BẮT BUỘC TỰ ĐỘNG GỘP LẠI thành 1 file
        /// Sau khi GỘP XONG THÌ XÓA TOÀN BỘ SPLIT FILE
        /// </summary>
        /// <param name="downloadUrls">Mảng các link tải về (theo thứ tự .001, .002, .003...)</param>
        /// <param name="outputFilePath">Đường dẫn file hoàn chỉnh sau khi ghép</param>
        /// <param name="displayName">Tên hiển thị (ví dụ: "Ghost of Tsushima", "Win 10 ISO")</param>
        /// <param name="runAfterMerge">Có chạy file sau khi ghép không (default: true)</param>
        /// <param name="deleteAfterMerge">Có xóa split file sau khi ghép không (default: true - BẮT BUỘC)</param>
        protected async Task InstallWithMultipartAsync(string[] downloadUrls, string outputFilePath, string displayName, bool runAfterMerge = true, bool deleteAfterMerge = true)
        {
            if (downloadUrls == null || downloadUrls.Length == 0)
            {
                UpdateStatus($"Lỗi: Không có link tải cho {displayName}", "Red");
                return;
            }

            try
            {
                // Step 1: Download all parts
                string[] partPaths = new string[downloadUrls.Length];
                string tempFolder = Path.Combine(GetGMTPCFolder(), $"{displayName}_Temp_{DateTime.Now:yyyyMMdd_HHmmss}");
                Directory.CreateDirectory(tempFolder);

                for (int i = 0; i < downloadUrls.Length; i++)
                {
                    string partFileName = Path.GetFileName(downloadUrls[i]);
                    partPaths[i] = Path.Combine(tempFolder, partFileName);
                    
                    UpdateStatus($"Đang tải {displayName} - Phần {i + 1}/{downloadUrls.Length}...", "Cyan");
                    await DownloadWithProgressAsync(downloadUrls[i], partPaths[i], $"{displayName} - Part {i + 1}");
                    
                    // Reset progress bar between parts
                    Dispatcher.Invoke(() =>
                    {
                        DownloadProgressBar.Value = 0;
                        ProgressTextBlock.Text = "";
                        SpeedTextBlock.Text = "";
                    });
                }

                // Step 2: Merge all parts (BẮT BUỘC)
                UpdateStatus($"Tải xong {downloadUrls.Length} phần! Đang gộp file...", "Cyan");
                await MergeSplitFilesAsync(partPaths, outputFilePath);

                // Step 3: Delete all split files (BẮT BUỘC)
                if (deleteAfterMerge)
                {
                    UpdateStatus("Đang xóa các file split...", "Gray");
                    foreach (string partPath in partPaths)
                    {
                        if (File.Exists(partPath))
                        {
                            try
                            {
                                File.Delete(partPath);
                                UpdateStatus($"Đã xóa {Path.GetFileName(partPath)}", "Gray");
                            }
                            catch { /* Ignore delete errors */ }
                        }
                    }
                    
                    // Delete temp folder
                    try
                    {
                        if (Directory.Exists(tempFolder))
                        {
                            Directory.Delete(tempFolder, true);
                            UpdateStatus($"Đã xóa folder tạm {tempFolder}", "Gray");
                        }
                    }
                    catch { /* Ignore delete errors */ }
                    
                    UpdateStatus($"Đã xóa toàn bộ {downloadUrls.Length} file split!", "Green");
                }

                // Step 4: Run file after merge (if requested)
                if (runAfterMerge && File.Exists(outputFilePath))
                {
                    UpdateStatus($"Đang mở {displayName}...", "Green");
                    Process.Start(new ProcessStartInfo 
                    { 
                        FileName = outputFilePath, 
                        UseShellExecute = true 
                    });
                }

                UpdateStatus($"Hoàn tất cài đặt {displayName}!", "Green");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài {displayName}: {ex.Message}", "Red");
                throw;
            }
        }

        /// <summary>
        /// Gộp nhiều split files thành 1 file hoàn chỉnh
        /// </summary>
        protected async Task MergeSplitFilesAsync(string[] partPaths, string outputPath)
        {
            using (var outputFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                foreach (string partPath in partPaths)
                {
                    using (var inputFs = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        await inputFs.CopyToAsync(outputFs);
                    }
                }
            }
            UpdateStatus($"Gộp file thành công: {Path.GetFileName(outputPath)}", "Green");
        }

        /// <summary>
        /// Cơ chế cài đặt Multi-part với retry logic
        /// </summary>
        protected async Task InstallWithMultipartAndRetryAsync(string[] downloadUrls, string outputFilePath, string displayName, int maxRetries = 3)
        {
            int retryCount = 0;
            while (retryCount < maxRetries)
            {
                try
                {
                    await InstallWithMultipartAsync(downloadUrls, outputFilePath, displayName);
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
