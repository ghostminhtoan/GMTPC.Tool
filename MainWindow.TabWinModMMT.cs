// AI Summary: 2026-03-14 - Replaced Mediafire download with GitHub 3-part download + auto merge logic
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        private void ChkWin10LtscIot21H2_Click(object sender, RoutedEventArgs e)
        {
            UpdateInstallButtonState();
        }

        private async Task InstallWin10LtscIot21H2Async()
        {
            // GitHub 3-part download links
            string[] partUrls = new string[]
            {
                "https://github.com/ghostminhtoan/MMT/releases/download/windows/LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.001",
                "https://github.com/ghostminhtoan/MMT/releases/download/windows/LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.002",
                "https://github.com/ghostminhtoan/MMT/releases/download/windows/LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.003"
            };

            string baseFileName = "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso";
            string destinationFolder = "C:\\";
            string[] partPaths = new string[]
            {
                System.IO.Path.Combine(destinationFolder, "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.001"),
                System.IO.Path.Combine(destinationFolder, "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.002"),
                System.IO.Path.Combine(destinationFolder, "LTSC.IOT.21H2.-.2021.10.-.gaming.-.Office.365.-.win.10.MMTPC.3.0.boot.windowsRE.iso.003")
            };
            string mergedIsoPath = System.IO.Path.Combine(destinationFolder, baseFileName);

            try
            {
                UpdateStatus("Đang tải 3 phần từ GitHub...", "Cyan");

                // Download all 3 parts
                await DownloadMultiPartAsync(partUrls, partPaths, "Win 10 LTSC IoT 21H2 - GitHub");

                UpdateStatus("Đã tải xong 3 phần, đang gộp file...", "Cyan");

                // Merge the parts using HJSplit command line (7-Zip or similar)
                // Using copy /b command for binary merge
                string mergeCommand = $"/c copy /b \"{partPaths[0]}\"+\"{partPaths[1]}\"+\"{partPaths[2]}\" \"{mergedIsoPath}\"";
                var mergeProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = mergeCommand,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                mergeProcess.WaitForExit();

                if (mergeProcess.ExitCode != 0)
                    throw new Exception("Lỗi gộp file ISO. Vui lòng thử lại.");

                UpdateStatus("Gộp file thành công! Đang xóa file tạm...", "Cyan");

                // Delete the 3 part files
                for (int i = 0; i < 3; i++)
                {
                    if (File.Exists(partPaths[i]))
                    {
                        try { File.Delete(partPaths[i]); } catch { }
                    }
                }

                UpdateStatus("Hoàn tất! Đang mở ổ C và file ISO...", "Green");
                Process.Start("explorer.exe", $"/select,{mergedIsoPath}");
                Process.Start(new ProcessStartInfo { FileName = mergedIsoPath, UseShellExecute = true });
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Đã hủy tải Win 10 LTSC IoT 21H2.", "Yellow");
                // Clean up partial downloads
                for (int i = 0; i < 3; i++)
                {
                    if (File.Exists(partPaths[i]))
                        try { File.Delete(partPaths[i]); } catch { }
                }
                if (File.Exists(mergedIsoPath))
                    try { File.Delete(mergedIsoPath); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi tải Win 10 LTSC IoT 21H2: {ex.Message}", "Red");
                throw;
            }
        }
    }
}
