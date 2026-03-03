using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Management;
using Microsoft.Win32;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Data;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {

        private async Task InstallPowerISOAsync()
        {
            try
            {
                UpdateStatus("Đang tải PowerISO...", "Cyan");
                string powerISOPath = Path.Combine(GetGMTPCFolder(), "PowerISO8.exe");
                await DownloadWithProgressAsync("https://www.poweriso.com/PowerISO8-x64.exe", powerISOPath, "PowerISO");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt PowerISO (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = powerISOPath,
                    Arguments = "/S",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt PowerISO hoàn tất!", "Green");
                }

                if (File.Exists(powerISOPath))
                {
                    File.Delete(powerISOPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt PowerISO: {ex.Message}", "Red");
            }
        }


        private async Task InstallVPN1111Async()
        {
            try
            {
                UpdateStatus("Đang tải vpn 1111...", "Cyan");
                string vpn1111Path = Path.Combine(GetGMTPCFolder(), "Cloudflare_1.1.1.1_Release-x64.msi");
                await DownloadWithProgressAsync("https://developers.cloudflare.com/warp-client/Cloudflare_1.1.1.1_Release-x64.msi", vpn1111Path, "vpn 1111");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt vpn 1111 (yêu cầu quyền)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $"/i \"{vpn1111Path}\" /passive",
                    UseShellExecute = true
                };

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt vpn 1111 hoàn tất!", "Green");
                }

                if (File.Exists(vpn1111Path))
                {
                    File.Delete(vpn1111Path);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt vpn 1111: {ex.Message}", "Red");
            }
        }


        private async Task InstallTeracopyAsync()
        {
            try
            {
                // Teracopy installer thương bi antivirus chặn or cnh báo, nhng ng dng yu cầu cài dặt
                UpdateStatus("Đang tải bô cài Teracopy...", "Cyan");
                string teraCopyPath = Path.Combine(GetGMTPCFolder(), "teracopy.exe");

                // Download file với tên teracopy.exe
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/teracopy.exe", teraCopyPath, "Teracopy Installer");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Thêm Windows Defender exclusion tạm thời cho %temp%
                string tempPath = Path.GetTempPath();
                try
                {
                    ProcessStartInfo addExclusionInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"Add-MpPreference -ExclusionPath '{tempPath}' -Force\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    Process addExclusionProcess = Process.Start(addExclusionInfo);
                    if (addExclusionProcess != null)
                    {
                        await Task.Run(() => addExclusionProcess.WaitForExit());
                        UpdateStatus("Đã thêm exclusion tạm thời cho %temp% để cài Teracopy", "Yellow");
                    }
                }
                catch (Exception exEx)
                {
                    UpdateStatus($"Cảnh báo: Không thể thêm exclusion: {exEx.Message}", "Yellow");
                }

                UpdateStatus("Đang cài đặt Teracopy (silent)...", "Yellow");
                ProcessStartInfo installInfo = new ProcessStartInfo
                {
                    FileName = teraCopyPath,
                    Arguments = "/s", // Silent mode
                    UseShellExecute = true
                };

                Process installProcess = Process.Start(installInfo);
                if (installProcess != null)
                {
                    await Task.Run(() => installProcess.WaitForExit());
                    if (installProcess.ExitCode == 0)
                    {
                        UpdateStatus("Cài đặt Teracopy hoàn tất!", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Cài đặt Teracopy hoàn tất với mã: {installProcess.ExitCode}", "Green");
                    }
                }

                // Xóa Windows Defender exclusion tạm thời cho %temp% sau khi cài xong
                try
                {
                    ProcessStartInfo removeExclusionInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"Remove-MpPreference -ExclusionPath '{tempPath}' -Force\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    Process removeExclusionProcess = Process.Start(removeExclusionInfo);
                    if (removeExclusionProcess != null)
                    {
                        await Task.Run(() => removeExclusionProcess.WaitForExit());
                        UpdateStatus("Đã xóa exclusion tạm thời cho %temp%", "Yellow");
                    }
                }
                catch (Exception exEx)
                {
                    UpdateStatus($"Cảnh báo: Không thể xóa exclusion: {exEx.Message}", "Yellow");
                }

                // Tạo Windows Defender exclusion vĩnh viễn cho %programfiles%\Teracopy
                try
                {
                    string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                    string teraCopyExclusionPath = Path.Combine(programFilesPath, "Teracopy");

                    ProcessStartInfo addPermanentExclusionInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"Add-MpPreference -ExclusionPath '{teraCopyExclusionPath}' -Force\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };
                    Process permanentExclusionProcess = Process.Start(addPermanentExclusionInfo);
                    if (permanentExclusionProcess != null)
                    {
                        await Task.Run(() => permanentExclusionProcess.WaitForExit());
                        UpdateStatus("Đã tạo Windows Defender exclusion vĩnh viễn cho Teracopy", "Yellow");
                    }
                }
                catch (Exception exEx)
                {
                    UpdateStatus($"Cảnh báo: Không thể tạo exclusion vĩnh viễn: {exEx.Message}", "Yellow");
                }

                // Xóa file installer sau khi chạy xong
                if (File.Exists(teraCopyPath))
                {
                    File.Delete(teraCopyPath);
                    UpdateStatus("Đã xóa file Teracopy installer tạm thời", "Cyan");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải hoặc cài đặt Teracopy: {ex.Message}", "Red");
            }
        }


        private async Task InstallGoogleDriveAsync()
        {
            try
            {
                UpdateStatus("Đang tải Google Drive...", "Cyan");
                string googleDrivePath = Path.Combine(GetGMTPCFolder(), "GoogleDriveSetup.exe");
                await DownloadWithProgressAsync("https://dl.google.com/drive-file-stream/GoogleDriveSetup.exe", googleDrivePath, "Google Drive");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt Google Drive...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = googleDrivePath,
                    Arguments = "--silent",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt Google Drive hoàn tất!", "Green");
                }

                if (File.Exists(googleDrivePath))
                {
                    File.Delete(googleDrivePath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Google Drive: {ex.Message}", "Red");
            }
        }


        // InstallNetLimiterAsync() -> Moved to MainWindow.SystemArguments.cs
        // (có /passive + ShowNetLimiterKeyDialog)


        private async Task InstallFolderSizeAsync()
        {
            try
            {
                UpdateStatus("Đang tải FolderSize...", "Cyan");
                string folderSizePath = Path.Combine(GetGMTPCFolder(), "FolderSize-2.6-x64.msi");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/FolderSize-2.6-x64.msi", folderSizePath, "FolderSize");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt FolderSize (yêu cầu quyền)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "msiexec",
                    Arguments = $"/i \"{folderSizePath}\" /passive",
                    UseShellExecute = true
                };

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt FolderSize hoàn tất!", "Green");
                }

                if (File.Exists(folderSizePath))
                {
                    File.Delete(folderSizePath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt FolderSize: {ex.Message}", "Red");
            }
        }


        // ShowNetLimiterKeyDialog() -> Moved to MainWindow.SystemArguments.cs
        // BtnActivateNetLimiter_Click() -> Moved to MainWindow.SystemArguments.cs


        private void ChkMMTApps_Click(object sender, RoutedEventArgs e)
        {
            if (ChkMMTApps.IsChecked == true)
            {
                UpdateStatus("Đã chọn: MMT Apps", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: MMT Apps", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkDISMPP_Click(object sender, RoutedEventArgs e)
        {
            if (ChkDISMPP.IsChecked == true)
            {
                UpdateStatus("Đã chọn: DISM++", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: DISM++", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkComfortClipboardPro_Click(object sender, RoutedEventArgs e)
        {
            if (ChkComfortClipboardPro.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Comfort Clipboard Pro", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Comfort Clipboard Pro", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkFolderSize_Click(object sender, RoutedEventArgs e)
        {
            if (ChkFolderSize.IsChecked == true)
            {
                UpdateStatus("Đã chọn: FolderSize", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: FolderSize", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkPowerISO_Click(object sender, RoutedEventArgs e)
        {
            if (ChkPowerISO.IsChecked == true)
            {
                UpdateStatus("Đã chọn: PowerISO", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: PowerISO", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkVPN1111_Click(object sender, RoutedEventArgs e)
        {
            if (ChkVPN1111.IsChecked == true)
            {
                UpdateStatus("Đã chọn: vpn 1111", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: vpn 1111", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkTeracopy_Click(object sender, RoutedEventArgs e)
        {
            if (ChkTeracopy.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Teracopy", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Teracopy", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkGoogleDrive_Click(object sender, RoutedEventArgs e)
        {
            if (ChkGoogleDrive.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Google Drive", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Google Drive", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkNetLimiter_Click(object sender, RoutedEventArgs e)
        {
            if (ChkNetLimiter.IsChecked == true)
            {
                UpdateStatus("Đã chọn: NetLimiter", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: NetLimiter", "Yellow");
            }

            UpdateInstallButtonState();
        }


        // InstallComfortClipboardProAsync() -> Moved to MainWindow.SystemArguments.cs
        // (có MessageBox.Show + /passive argument)


        // BtnBackupRestoreMklinkMMT_Click() -> Moved to MainWindow.SystemArguments.cs
        // InstallMMTAppsAsync() -> Moved to MainWindow.SystemArguments.cs
        // (có MessageBox.Show + /passive argument)
        // BtnDefenderControl_Click() -> Moved to MainWindow.SystemArguments.cs
        // (có MessageBox.Show + VBS + /s -p1111)


    }
}
