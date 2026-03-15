using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HtmlAgilityPack;

namespace GMTPC.Tool
{
    // =============================================================================
    // MainWindow.TabWinModMMT.cs
    // Updated: 2026-03-15 - Changed from Mediafire to GitHub (3-part segmented download)
    // =============================================================================
    public partial class MainWindow
    {
        // GitHub download URLs for Win 10 LTSC IOT 21H2 (3 parts)
        private const string WIN10_LTSC_IOT_PART1_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.001";
        private const string WIN10_LTSC_IOT_PART2_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.002";
        private const string WIN10_LTSC_IOT_PART3_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.003";
        private const string WIN10_LTSC_IOT_FINAL_NAME = "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso";

        private void ChkWin10LtscIot21H2_Click(object sender, RoutedEventArgs e)
        {
            UpdateInstallButtonState();
        }

        private async Task InstallWin10LtscIot21H2Async()
        {
            string gmtPCFolder = GetGMTPCFolder();
            string part1Path = Path.Combine(gmtPCFolder, "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.001");
            string part2Path = Path.Combine(gmtPCFolder, "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.002");
            string part3Path = Path.Combine(gmtPCFolder, "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.003");
            string finalIsoPath = Path.Combine(gmtPCFolder, WIN10_LTSC_IOT_FINAL_NAME);

            UpdateStatus("Đang tải về 3 file từ GitHub (ổ C)...", "Cyan");

            try
            {
                // Download all 3 parts sequentially with progress
                UpdateStatus("Đang tải phần 1/3...", "Cyan");
                await DownloadWithProgressAsync(WIN10_LTSC_IOT_PART1_URL, part1Path, "Phần 1/3 - GitHub");

                UpdateStatus("Đang tải phần 2/3...", "Cyan");
                await DownloadWithProgressAsync(WIN10_LTSC_IOT_PART2_URL, part2Path, "Phần 2/3 - GitHub");

                UpdateStatus("Đang tải phần 3/3...", "Cyan");
                await DownloadWithProgressAsync(WIN10_LTSC_IOT_PART3_URL, part3Path, "Phần 3/3 - GitHub");

                UpdateStatus("Tải xong 3 phần! Đang gộp file...", "Cyan");

                // Merge the 3 parts into final ISO
                await MergeIsoPartsAsync(part1Path, part2Path, part3Path, finalIsoPath);

                UpdateStatus("Gộp file thành công! Đang mở thư mục và file ISO...", "Green");
                
                // Open folder with the final ISO
                Process.Start("explorer.exe", $"/select,{finalIsoPath}");
                
                // Mount/open the ISO
                Process.Start(new ProcessStartInfo { FileName = finalIsoPath, UseShellExecute = true });
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Đã hủy tải Win 10 LTSC IoT 21H2.", "Yellow");
                // Cleanup partial downloads
                CleanupPartialDownloads(part1Path, part2Path, part3Path, finalIsoPath);
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi tải Win 10 LTSC IoT 21H2: {ex.Message}", "Red");
                CleanupPartialDownloads(part1Path, part2Path, part3Path, finalIsoPath);
                throw;
            }
        }

        /// <summary>
        /// Merge 3 ISO parts into a single ISO file.
        /// </summary>
        private async Task MergeIsoPartsAsync(string part1Path, string part2Path, string part3Path, string outputPath)
        {
            string[] parts = { part1Path, part2Path, part3Path };
            
            using (var outputFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                foreach (string partPath in parts)
                {
                    using (var inputFs = new FileStream(partPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        await inputFs.CopyToAsync(outputFs);
                    }
                }
            }

            // Delete the 3 part files after successful merge
            foreach (string partPath in parts)
            {
                if (File.Exists(partPath))
                {
                    try
                    {
                        File.Delete(partPath);
                        UpdateStatus($"Đã xóa file {Path.GetFileName(partPath)}", "Gray");
                    }
                    catch { /* Ignore delete errors */ }
                }
            }

            UpdateStatus("Đã xóa 3 file tạm sau khi gộp!", "Green");
        }

        /// <summary>
        /// Cleanup partial download files on error or cancellation.
        /// </summary>
        private void CleanupPartialDownloads(params string[] paths)
        {
            foreach (string path in paths)
            {
                if (File.Exists(path))
                {
                    try { File.Delete(path); } catch { }
                }
            }
        }
    }
}
