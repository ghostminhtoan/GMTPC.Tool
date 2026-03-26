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
    // Updated: 2026-03-17 - Changed download URLs to new links without boot.windowsRE
    // =============================================================================
    public partial class MainWindow
    {
        // GitHub download URLs for Win 10 LTSC IOT 21H2 (3 parts)
        private const string WIN10_LTSC_IOT_PART1_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.iso.001";
        private const string WIN10_LTSC_IOT_PART2_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.iso.002";
        private const string WIN10_LTSC_IOT_PART3_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.iso.003";
        private const string WIN10_LTSC_IOT_FINAL_NAME = "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.iso";

        // GitHub download URLs for Win 10 22H2 2024 DECEMBER (5 parts)
        private const string WIN10_22H2_2024_DEC_PART1_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.001";
        private const string WIN10_22H2_2024_DEC_PART2_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.002";
        private const string WIN10_22H2_2024_DEC_PART3_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.003";
        private const string WIN10_22H2_2024_DEC_PART4_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.004";
        private const string WIN10_22H2_2024_DEC_PART5_URL = "https://github.com/ghostminhtoan/MMT/releases/download/windows/win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.005";
        private const string WIN10_22H2_2024_DEC_FINAL_NAME = "win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso";

        // WintoHDD - Use InstallWithPromptAsync for Yes/No dialog with NTFS Compression check
        private async Task InstallWintoHDDAsync()
        {
            // Check and enable NTFS Compression before installation (only if running as admin)
            await CheckAndEnableNTFSCompression();

            string gmtPCFolder = GetGMTPCFolder();
            string wintoHddPath = Path.Combine(gmtPCFolder, "wintohdd.exe");
            await InstallWithPromptAsync(WINTOHDD_DOWNLOAD_URL, wintoHddPath, WINTOHDD_INSTALL_ARGUMENTS, "WintoHDD");
        }

        /// <summary>
        /// Check if NTFS Compression is enabled on system drive. If not and running as admin, enable it.
        /// WintoHDD requires NTFS Compression to be enabled.
        /// If not running as admin, skip the check to avoid UAC prompt.
        /// </summary>
        private async Task CheckAndEnableNTFSCompression()
        {
            try
            {
                UpdateStatus("Đang kiểm tra NTFS Compression...", "Cyan");
                await Task.Delay(500);

                // Check if running as administrator
                bool isAdmin = IsRunningAsAdministrator();

                if (!isAdmin)
                {
                    // Not running as admin - skip NTFS compression check to avoid UAC prompt
                    // WintoHDD will handle this itself with its own UAC prompt
                    UpdateStatus("Không có quyền admin, bỏ qua kiểm tra NTFS Compression.", "Gray");
                    await Task.Delay(500);
                    return;
                }

                // Check NTFS Compression status using compact /compactstate
                ProcessStartInfo checkStartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c compact /compactstate:C:",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process checkProcess = Process.Start(checkStartInfo);
                string output = checkProcess.StandardOutput.ReadToEnd();
                checkProcess.WaitForExit();

                // Parse output to check compression status
                // Output format: "CompactState on C: is Yes" or "CompactState on C: is No"
                bool isCompressionEnabled = output.Contains("Yes") || output.Contains("1");

                if (!isCompressionEnabled)
                {
                    UpdateStatus("NTFS Compression đang tắt. Đang bật (có thể mất vài phút)...", "Yellow");
                    await Task.Delay(500);

                    // Enable NTFS Compression on Windows folder using compact
                    // /C = compress, /I = continue on error, /F = force, /Q = quiet
                    // /S = recurse subdirectories
                    ProcessStartInfo enableStartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = "/c compact /C /I /F /Q /S:C:\\Windows",
                        UseShellExecute = true,
                        CreateNoWindow = false // Show progress window
                    };

                    Process enableProcess = Process.Start(enableStartInfo);
                    if (enableProcess != null)
                    {
                        await Task.Run(() => enableProcess.WaitForExit());
                        UpdateStatus("Đã bật NTFS Compression thành công!", "Green");
                        await Task.Delay(1000);
                    }
                }
                else
                {
                    UpdateStatus("NTFS Compression đã được bật.", "Green");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi kiểm tra/bật NTFS Compression: {ex.Message}", "Red");
                await Task.Delay(500);
            }
        }

        private void ChkWintoHDD_Click(object sender, RoutedEventArgs e)
        {
            if (ChkWintoHDD.IsChecked == true)
            {
                UpdateStatus("Đã chọn: WintoHDD", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: WintoHDD", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkWin10LtscIot21H2_Click(object sender, RoutedEventArgs e)
        {
            UpdateInstallButtonState();
        }

        private void ChkWin10_22H2_2024_December_Click(object sender, RoutedEventArgs e)
        {
            UpdateInstallButtonState();
        }

        private async Task InstallWin10LtscIot21H2Async()
        {
            string gmtPCFolder = GetGMTPCFolder();
            string part1Path = Path.Combine(gmtPCFolder, "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.iso.001");
            string part2Path = Path.Combine(gmtPCFolder, "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.iso.002");
            string part3Path = Path.Combine(gmtPCFolder, "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.iso.003");
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

                // Merge the 3 parts into final ISO and delete parts after merging
                await MergeIsoPartsAsync(part1Path, part2Path, part3Path, finalIsoPath, deleteParts: true);

                UpdateStatus("Gộp file thành công! Đang mở thư mục và file ISO...", "Green");
                
                // Open folder with the final ISO
                Process.Start("explorer.exe", $"/select,{finalIsoPath}");

                // Mount/open the ISO
                Process.Start(new ProcessStartInfo { FileName = finalIsoPath, UseShellExecute = true });
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Đã hủy tải Win 10 LTSC IoT 21H2.", "Yellow");
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi tải Win 10 LTSC IoT 21H2: {ex.Message}", "Red");
                throw;
            }
        }

        /// <summary>
        /// Merge 3 ISO parts into a single ISO file.
        /// </summary>
        private async Task MergeIsoPartsAsync(string part1Path, string part2Path, string part3Path, string outputPath, bool deleteParts = true)
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
            if (deleteParts)
            {
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
                UpdateStatus("Đã xóa 3 file split sau khi gộp!", "Green");
            }
        }

        // ===================================================================
        // Win 10 22H2 2024 DECEMBER - 5 parts ISO download
        // ===================================================================
        private async Task InstallWin10_22H2_2024_DecemberAsync()
        {
            string gmtPCFolder = GetGMTPCFolder();
            string part1Path = Path.Combine(gmtPCFolder, "win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.001");
            string part2Path = Path.Combine(gmtPCFolder, "win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.002");
            string part3Path = Path.Combine(gmtPCFolder, "win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.003");
            string part4Path = Path.Combine(gmtPCFolder, "win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.004");
            string part5Path = Path.Combine(gmtPCFolder, "win.10.22h2.2024.DECEMBER.-.Office.365.-.win.10.MMTPC.4.0.iso.005");
            string finalIsoPath = Path.Combine(gmtPCFolder, WIN10_22H2_2024_DEC_FINAL_NAME);

            UpdateStatus("Đang tải về 5 file từ GitHub (ổ C)...", "Cyan");

            try
            {
                // Download all 5 parts sequentially with progress
                UpdateStatus("Đang tải phần 1/5...", "Cyan");
                await DownloadWithProgressAsync(WIN10_22H2_2024_DEC_PART1_URL, part1Path, "Phần 1/5 - GitHub");

                UpdateStatus("Đang tải phần 2/5...", "Cyan");
                await DownloadWithProgressAsync(WIN10_22H2_2024_DEC_PART2_URL, part2Path, "Phần 2/5 - GitHub");

                UpdateStatus("Đang tải phần 3/5...", "Cyan");
                await DownloadWithProgressAsync(WIN10_22H2_2024_DEC_PART3_URL, part3Path, "Phần 3/5 - GitHub");

                UpdateStatus("Đang tải phần 4/5...", "Cyan");
                await DownloadWithProgressAsync(WIN10_22H2_2024_DEC_PART4_URL, part4Path, "Phần 4/5 - GitHub");

                UpdateStatus("Đang tải phần 5/5...", "Cyan");
                await DownloadWithProgressAsync(WIN10_22H2_2024_DEC_PART5_URL, part5Path, "Phần 5/5 - GitHub");

                UpdateStatus("Tải xong 5 phần! Đang gộp file...", "Cyan");

                // Merge the 5 parts into final ISO and delete parts after merging
                await MergeIsoPartsAsync(part1Path, part2Path, part3Path, part4Path, part5Path, finalIsoPath, deleteParts: true);

                UpdateStatus("Gộp file thành công! Đang mở thư mục và file ISO...", "Green");

                // Open folder with the final ISO
                Process.Start("explorer.exe", $"/select,{finalIsoPath}");

                // Mount/open the ISO
                Process.Start(new ProcessStartInfo { FileName = finalIsoPath, UseShellExecute = true });
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Đã hủy tải Win 10 22H2 2024 DECEMBER.", "Yellow");
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi tải Win 10 22H2 2024 DECEMBER: {ex.Message}", "Red");
                throw;
            }
        }

        /// <summary>
        /// Merge 5 ISO parts into a single ISO file.
        /// </summary>
        private async Task MergeIsoPartsAsync(string part1Path, string part2Path, string part3Path, string part4Path, string part5Path, string outputPath, bool deleteParts = true)
        {
            string[] parts = { part1Path, part2Path, part3Path, part4Path, part5Path };

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

            // Delete the 5 part files after successful merge
            if (deleteParts)
            {
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
                UpdateStatus("Đã xóa 5 file split sau khi gộp!", "Green");
            }
        }
    }
}
