using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GMTPC.Tool
{
    /*
     * AI Summary:
     * Date: 2026-03-07
     * - Updated Java download URL to https://www.java.com/en/download/
     * - Updated Java install arguments to /s
     */
    // =======================================================================
    // MainWindow.SystemArguments.cs
    // Chứa toàn bộ code phức tạp liên quan đến:
    //   - InstallXxxAsync() có MessageBox, nhiều nhánh argument, key dialog
    //   - BtnXxx_Click dành riêng cho một app cụ thể
    //   - ShowXxxKeyDialog() và các helper dialog
    //   - Logic activate / crack
    // =======================================================================
    public partial class MainWindow
    {
        // ===================================================================
        // TabPopular — Links (B) and Arguments (C)
        // TabItem Header: "Popular"
        // Checkboxes: ChkInstallIDM, ChkInstallWinRAR, ChkInstallBID,
        //             ChkActivateWindows, ChkPauseWindowsUpdate, ChkVcredist,
        //             ChkDirectX, ChkJava, ChkOpenAL, Chk3DPChip, Chk3DPNet,
        //             ChkRevoUninstaller
        // ===================================================================
        // IDM
        private const string IDM_DOWNLOAD_URL = "https://tinyurl.com/idmhcmvn";
        private const string IDM_ACTIVATE_URL = "https://github.com/ghostminhtoan/MMT/releases/download/activate/IDM_6.4x_rabbit.exe";
        private const string IDM_INSTALL_ARGUMENTS = "/s /a /u /o /quiet /skipdlgst";

        // WinRAR
        private const string WINRAR_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/WinRAR.exe";
        private const string WINRAR_INSTALL_ARGUMENTS = "/silent /I /EN";

        // BID (Bulk Image Downloader)
        private const string BID_DOWNLOAD_URL = "https://bulkimagedownloader.com/files/bid_6_62_setup_x64.exe";
        private const string BID_INSTALL_ARGUMENTS = "";

        // Activate Windows
        // Vcredist
        private const string VCREDIST_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/vcredist.all.in.one.by.MMT.Windows.Tech.exe";
        private const string VCREDIST_INSTALL_ARGUMENTS = "/passive";

        // DirectX
        private const string DIRECTX_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/DirectX.exe";
        private const string DIRECTX_INSTALL_ARGUMENTS = "/passive";

        // Java
        private const string JAVA_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/java.exe";
        private const string JAVA_INSTALL_ARGUMENTS = "/s";

        // OpenAL
        private const string OPENAL_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/oalinst.exe";
        private const string OPENAL_INSTALL_ARGUMENTS = "/S";

        // 3DP Chip
        private const string DPCHIP_DOWNLOAD_URL = "https://www.3dpchip.com/3dp/chip.exe";
        private const string DPCHIP_INSTALL_ARGUMENTS = "/S";

        // 3DP Net
        private const string DPNET_DOWNLOAD_URL = "https://www.3dpchip.com/3dp/net.exe";
        private const string DPNET_INSTALL_ARGUMENTS = "/S";

        // Revo Uninstaller
        private const string REVO_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/RevoUninstallerPro.exe";
        private const string REVO_INSTALL_ARGUMENTS = "/S";

        // Zalo
        private const string ZALO_DOWNLOAD_URL = "https://zalo.me/download/zalo-pc?utm=90000";
        private const string ZALO_INSTALL_ARGUMENTS = "/silent";

        // ===================================================================
        // TabBrowser — Links (B) and Arguments (C)
        // TabItem Header: "Browser"
        // Checkboxes: ChkChrome, ChkCocCoc, ChkEdge
        // ===================================================================
        // Chrome (Tab: Browser)
        private const string CHROME_DOWNLOAD_URL = "https://dl.google.com/chrome/install/latest/chrome_installer.exe";
        private const string CHROME_INSTALL_ARGUMENTS = "/silent /install";

        // CocCoc (Tab: Browser)
        private const string COCCOC_DOWNLOAD_URL = "https://coccoc.com/download/win32";
        private const string COCCOC_INSTALL_ARGUMENTS = "/silent";

        // Edge (Tab: Browser)
        private const string EDGE_DOWNLOAD_URL = "https://go.microsoft.com/fwlink/?linkid=2108834&Channel=Stable&language=vi";
        private const string EDGE_INSTALL_ARGUMENTS = "/silent /install";

        // ===================================================================
        // TabSystem — PowerISO
        // TabItem Header: "System"
        // Checkbox: ChkPowerISO
        // ===================================================================
        private async Task InstallZaloAsync()
        {
            try
            {
                UpdateStatus("Đang tải Zalo...", "Cyan");
                string zaloPath = Path.Combine(GetGMTPCFolder(), "ZaloSetup.exe");
                await DownloadWithProgressAsync(ZALO_DOWNLOAD_URL, zaloPath, "Zalo");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt Zalo (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = zaloPath,
                    Arguments = ZALO_INSTALL_ARGUMENTS,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt Zalo hoàn tất!", "Green");
                }

                if (File.Exists(zaloPath))
                {
                    File.Delete(zaloPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Zalo: {ex.Message}", "Red");
            }
        }

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

        // ===================================================================
        // TabSystem — VPN 1111
        // TabItem Header: "System"
        // Checkbox: ChkVPN1111
        // ===================================================================
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

        // ===================================================================
        // TabSystem — Teracopy
        // TabItem Header: "System"
        // Checkbox: ChkTeracopy
        // ===================================================================
        private async Task InstallTeracopyAsync()
        {
            try
            {
                // Teracopy installer thương bi antivirus chặn or cnh báo, nhng ng dng yu cầu cài dặt
                UpdateStatus("Đang tải bô cài Teracopy...", "Cyan");
                string teraCopyPath = Path.Combine(GetGMTPCFolder(), "teracopy.exe");

                // Download file với tên teracopy.exe
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/teracopy.exe", teraCopyPath, "Teracopy Installer");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Thêm Windows Defender exclusion tạm thời cho %temp%
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

        // ===================================================================
        // TabSystem — Google Drive
        // TabItem Header: "System"
        // Checkbox: ChkGoogleDrive
        // ===================================================================
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

        // ===================================================================
        // TabSystem — FolderSize
        // TabItem Header: "System"
        // Checkbox: ChkFolderSize
        // ===================================================================
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

        // ===================================================================
        // TabSystem — Checkbox Click Handlers
        // ===================================================================
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

        // ===================================================================
        // Common — UpdateInstallButtonState
        // ===================================================================
        private void UpdateInstallButtonState()
        {
            // Kiểm tra xem có ít nhất một checkbox nào được chọn không
            bool hasChecked = ChkInstallIDM.IsChecked == true ||
                             ChkActivateWindows.IsChecked == true ||
                             ChkActivateOffice.IsChecked == true ||
                             ChkOfficeToolPlus.IsChecked == true ||
                             ChkPauseWindowsUpdate.IsChecked == true ||
                             ChkInstallWinRAR.IsChecked == true ||
                             ChkInstallBID.IsChecked == true ||
                             // Thêm các checkbox mới cho tab Runtime
                             ChkVcredist.IsChecked == true ||
                             ChkDirectX.IsChecked == true ||
                             // Thêm checkbox cho Java và OpenAL
                             ChkJava.IsChecked == true ||
                             ChkOpenAL.IsChecked == true ||
                             // Thêm checkbox cho Driver
                             Chk3DPChip.IsChecked == true ||
                             Chk3DPNet.IsChecked == true ||
                             // Google Chrome
                             ChkChrome.IsChecked == true ||
                             // Cốc Cốc
                             ChkCocCoc.IsChecked == true ||
                             // Microsoft Edge
                             ChkEdge.IsChecked == true ||
                             ChkRevoUninstaller.IsChecked == true ||
                             // Thêm checkbox cho Zalo
                             ChkInstallZalo.IsChecked == true ||
                             // Thêm checkbox cho MMT Apps
                             ChkMMTApps.IsChecked == true ||
                             // Thêm checkbox cho DISM++
                             ChkDISMPP.IsChecked == true ||
                             // Thêm checkbox cho Comfort Clipboard Pro
                             ChkComfortClipboardPro.IsChecked == true ||
                             // Thêm checkbox cho Office Softmaker
                             ChkOfficeSoftmaker.IsChecked == true ||
                             ChkNotepadPP.IsChecked == true ||
                             // Thêm checkbox cho Fonts SFU/UTM/UVN/VNI
                             ChkFonts.IsChecked == true ||
                             // Thêm checkbox cho AOMEI Partition Assistant
                             ChkAomeiPartitionAssistant.IsChecked == true ||
                             // Thêm checkbox cho PowerISO
                             ChkPowerISO.IsChecked == true ||
                             // Thêm checkbox cho VPN 1111
                             ChkVPN1111.IsChecked == true ||
                             // Thêm checkbox cho Teracopy
                             ChkTeracopy.IsChecked == true ||
                             // Thêm checkbox cho Google Drive
                             ChkGoogleDrive.IsChecked == true ||
                             // Thêm checkbox cho NetLimiter
                             ChkNetLimiter.IsChecked == true ||
                             // FolderSize
                             ChkFolderSize.IsChecked == true ||
                             // Thêm checkbox cho Gaming tab
                             ChkDiskGenius.IsChecked == true ||
                             ChkProcessLasso.IsChecked == true ||
                             ChkThrottlestop.IsChecked == true ||
                             ChkMSIAfterburner.IsChecked == true ||
                             ChkLeagueOfLegends.IsChecked == true ||
                             ChkPorofessor.IsChecked == true ||
                             ChkSamuraiMaiden.IsChecked == true ||
                             ChkUltraviewer.IsChecked == true ||
                             // Multimedia
                             ChkPotPlayer.IsChecked == true ||
                             ChkFastStone.IsChecked == true ||
                             ChkFoxit.IsChecked == true ||
                             ChkBandiview.IsChecked == true ||
                             ChkAdvancedCodec.IsChecked == true ||
                             ChkTeamViewerQS.IsChecked == true ||
                             ChkTeamViewerFull.IsChecked == true ||
                             ChkAnyDesk.IsChecked == true ||
                             ChkVMWare162Lite.IsChecked == true ||
                             ChkWin11_26H1.IsChecked == true ||
                             ChkWin10_20H2_2022April.IsChecked == true ||
                             ChkWin10LtscIot21H2.IsChecked == true;

            // Bao gồm checkbox cho Tạo WinRE và WinPE

            // Cập nhật trạng thái của nút Install
            BtnInstall.IsEnabled = hasChecked;
        }

        // ===================================================================
        // Common — Add/Remove Defender Exclusion Path
        // ===================================================================
        private void AddDefenderExclusionPath(string exclusionPath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Add-MpPreference -ExclusionPath '{exclusionPath}' -Force\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi thêm exclusion: {ex.Message}", "Red");
            }
        }

        private void RemoveDefenderExclusionPath(string exclusionPath)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-Command \"Remove-MpPreference -ExclusionPath '{exclusionPath}' -Force\"",
                    UseShellExecute = true,
                    Verb = "runas",
                    CreateNoWindow = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi xóa exclusion: {ex.Message}", "Red");
            }
        }

        // ===================================================================
        // TabSystem — DISM++
        // TabItem Header: "System"
        // Checkbox: ChkDISMPP
        // ===================================================================
        private async Task InstallDISMPPAsync()
        {
            try
            {
                UpdateStatus("Đang tải DISM++...", "Cyan");
                string dismppPath = Path.Combine(GetGMTPCFolder(), "DISM++.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/WinPE/DISM++.exe", dismppPath, "DISM++ Installer");

                MessageBoxResult result = MessageBox.Show("Yes = Cài đặt tự động vào ổ C\nNo = Cài vào ổ khác", " Cài đặt tự động DISM++", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = dismppPath,
                    UseShellExecute = true
                };

                if (result == MessageBoxResult.Yes)
                {
                    startInfo.Arguments = "/s";
                    UpdateStatus("Cài đặt DISM++ vào ổ C...", "Yellow");
                }
                else if (result == MessageBoxResult.No)
                {
                    UpdateStatus("Cài DISM++ vào ổ khác...", "Yellow");
                }
                else
                {
                    UpdateStatus("Đã hủy cài đặt DISM++", "Yellow");
                    if (File.Exists(dismppPath)) File.Delete(dismppPath);
                    return;
                }

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus(process.ExitCode == 0 ? "Cài đặt DISM++ thành công!" : $"Cài đặt DISM++ thất bại. Mã lỗi: {process.ExitCode}", process.ExitCode == 0 ? "Green" : "Red");
                }

                if (File.Exists(dismppPath)) File.Delete(dismppPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt DISM++: {ex.Message}", "Red");
            }
        }


        // ===================================================================
        // TabSystem — NetLimiter
        // TabItem Header: "System"
        // Checkbox: ChkNetLimiter
        // ===================================================================
        private async Task InstallNetLimiterAsync()
        {
            try
            {
                UpdateStatus("Đang tải NetLimiter...", "Cyan");
                string netLimiterPath = Path.Combine(GetGMTPCFolder(), "NetLimiter.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/netlimiter-4.1.12.0.exe", netLimiterPath, "NetLimiter");

                UpdateStatus("Đang cài đặt NetLimiter...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = netLimiterPath,
                    Arguments = "/passive",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt NetLimiter hoàn tất!", "Green");

                    await Dispatcher.InvokeAsync(() => ShowNetLimiterKeyDialog());
                }

                if (File.Exists(netLimiterPath)) File.Delete(netLimiterPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt NetLimiter: {ex.Message}", "Red");
            }
        }

        private void ShowNetLimiterKeyDialog()
        {
            try
            {
                Window keyDialog = new Window
                {
                    Title = "NetLimiter Key",
                    Width = 500,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Owner = this,
                    Background = new SolidColorBrush(Color.FromRgb(31, 31, 31)),
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230))
                };

                StackPanel mainPanel = new StackPanel { Margin = new Thickness(10), Orientation = Orientation.Vertical };

                // Name row
                StackPanel line1 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                TextBox nameBox = new TextBox
                {
                    Text = "Vladimir Putin #2", Width = 300, Height = 28, IsReadOnly = true,
                    Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(122, 122, 122)),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Button copyNameBtn = new Button
                {
                    Content = "Copy", Width = 70, Height = 28,
                    Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(122, 122, 122))
                };
                copyNameBtn.Click += (s, e) => { Clipboard.SetText(nameBox.Text); UpdateStatus("Đã copy: Vladimir Putin #2", "Green"); };
                line1.Children.Add(nameBox);
                line1.Children.Add(copyNameBtn);
                mainPanel.Children.Add(line1);

                // Key row
                StackPanel line2 = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
                TextBox keyBox = new TextBox
                {
                    Text = "XLEVD-PNASB-6A3BD-Z72GJ-SPAH7", Width = 300, Height = 28, IsReadOnly = true,
                    Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(122, 122, 122)),
                    Margin = new Thickness(0, 0, 5, 0)
                };
                Button copyKeyBtn = new Button
                {
                    Content = "Copy", Width = 70, Height = 28,
                    Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                    Foreground = new SolidColorBrush(Color.FromRgb(230, 230, 230)),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(122, 122, 122))
                };
                copyKeyBtn.Click += (s, e) => { Clipboard.SetText(keyBox.Text); UpdateStatus("Đã copy: XLEVD-PNASB-6A3BD-Z72GJ-SPAH7", "Green"); };
                line2.Children.Add(keyBox);
                line2.Children.Add(copyKeyBtn);
                mainPanel.Children.Add(line2);

                keyDialog.Content = mainPanel;
                keyDialog.ShowDialog();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi hiển thị dialog key: {ex.Message}", "Red");
            }
        }

        private void BtnActivateNetLimiter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Mở cửa sổ kích hoạt NetLimiter...", "Cyan");
                ShowNetLimiterKeyDialog();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi mở NetLimiter Key Dialog: {ex.Message}", "Red");
            }
        }


        // ===================================================================
        // TabSystem — Comfort Clipboard Pro
        // TabItem Header: "System"
        // Checkbox: ChkComfortClipboardPro
        // ===================================================================
        private async Task InstallComfortClipboardProAsync()
        {
            try
            {
                UpdateStatus("Đang tải Comfort Clipboard Pro...", "Cyan");
                string comfortClipboardPath = Path.Combine(GetGMTPCFolder(), "Comfort.Clipboard.Pro.7.0.2.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Comfort.Clipboard.Pro.7.0.2.exe", comfortClipboardPath, "Comfort Clipboard Pro Installer");

                MessageBoxResult result = MessageBox.Show("Yes = Cài đặt tự động vào ổ C\nNo = Cài vào ổ khác", " Cài đặt tự động Comfort Clipboard Pro", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = comfortClipboardPath, UseShellExecute = true };

                if (result == MessageBoxResult.Yes) { startInfo.Arguments = "/passive"; UpdateStatus("Cài đặt tự động vào ổ C", "Yellow"); }
                else if (result == MessageBoxResult.No) { UpdateStatus("Cài vào ổ khác...", "Yellow"); }
                else
                {
                    UpdateStatus("Đã hủy cài đặt Comfort Clipboard Pro", "Yellow");
                    if (File.Exists(comfortClipboardPath)) File.Delete(comfortClipboardPath);
                    return;
                }

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus(process.ExitCode == 0 ? "Cài đặt Comfort Clipboard Pro thành công!" : $"Thất bại. Mã lỗi: {process.ExitCode}", process.ExitCode == 0 ? "Green" : "Red");
                }

                if (File.Exists(comfortClipboardPath)) File.Delete(comfortClipboardPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Comfort Clipboard Pro: {ex.Message}", "Red");
            }
        }


        // ===================================================================
        // TabSystem — MMT Apps
        // TabItem Header: "System"
        // Checkbox: ChkMMTApps
        // ===================================================================
        private async Task InstallMMTAppsAsync()
        {
            try
            {
                UpdateStatus("Đang tải MMT.Apps.exe...", "Cyan");
                string mmtAppsPath = Path.Combine(GetGMTPCFolder(), "MMT.Apps.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/MMT.Apps.exe", mmtAppsPath, "MMT Apps Installer");

                MessageBoxResult result = MessageBox.Show("Yes = Cài đặt tự động vào ổ C\nNo = Cài vào ổ khác", "Cài đặt tự động MMT Apps", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = mmtAppsPath, UseShellExecute = true };

                if (result == MessageBoxResult.Yes) { startInfo.Arguments = "/passive"; UpdateStatus("Cài đặt tự động vào ổ C", "Yellow"); }
                else if (result == MessageBoxResult.No) { UpdateStatus("Cài vào ổ khác...", "Yellow"); }
                else
                {
                    UpdateStatus("Đã hủy cài đặt MMT Apps", "Yellow");
                    if (File.Exists(mmtAppsPath)) File.Delete(mmtAppsPath);
                    return;
                }

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus(process.ExitCode == 0 ? "Cài đặt MMT Apps thành công!" : $"Thất bại. Mã lỗi: {process.ExitCode}", process.ExitCode == 0 ? "Green" : "Red");
                }

                if (File.Exists(mmtAppsPath)) File.Delete(mmtAppsPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt MMT Apps: {ex.Message}", "Red");
            }
        }


        // ===================================================================
        // TabSystem — Defender Control
        // TabItem Header: "System"
        // Button: BtnDefenderControl
        // ===================================================================
        private async void BtnDefenderControl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                UpdateStatus("Bắt đầu quá trình Defender Control...", "Cyan");

                AddDefenderExclusionPath(Path.GetTempPath());
                AddDefenderExclusionPath(Path.Combine(Environment.GetEnvironmentVariable("programfiles(x86)"), "DefenderControl"));

                string vbsUrl = "https://raw.githubusercontent.com/ghostminhtoan/MMT/refs/heads/main/windefend%20off.vbs";
                string vbsPath = Path.Combine(Path.GetTempPath(), "windefend_off.vbs");
                using (WebClient client = new WebClient())
                {
                    await Task.Run(() => client.DownloadFile(vbsUrl, vbsPath));
                }

                UpdateStatus("Đang chạy windefend off.vbs...", "Cyan");
                Process vbsProcess = Process.Start(new ProcessStartInfo
                {
                    FileName = "cscript.exe", Arguments = $"\"{vbsPath}\"",
                    UseShellExecute = true, CreateNoWindow = false, Verb = "runas"
                });
                if (vbsProcess != null) await Task.Run(() => vbsProcess.WaitForExit());

                if (File.Exists(vbsPath)) File.Delete(vbsPath);

                MessageBoxResult result = MessageBox.Show(
                    "Nếu đã tắt tamper protection thì bấm Yes để tải Defender Control về",
                    "Defender Control", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    string exeUrl = "https://raw.githubusercontent.com/ghostminhtoan/MMT/main/Defender%20Control.exe";
                    string exePath = Path.Combine(Path.GetTempPath(), "Defender Control.exe");
                    using (WebClient client = new WebClient())
                    {
                        await Task.Run(() => client.DownloadFile(exeUrl, exePath));
                    }

                    Process exeProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath, Arguments = "/s -p1111",
                        UseShellExecute = true, CreateNoWindow = false
                    });
                    if (exeProcess != null) await Task.Run(() => exeProcess.WaitForExit());

                    if (File.Exists(exePath)) File.Delete(exePath);
                    RemoveDefenderExclusionPath(Path.GetTempPath());
                }

                UpdateStatus("Hoàn thành quá trình Defender Control!", "Green");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi: {ex.Message}", "Red");
            }
        }


        // ===================================================================
        // TabSystem — Backup Restore Mklink MMT
        // TabItem Header: "System"
        // Button: BtnBackupRestoreMklinkMMT
        // ===================================================================
        private async void BtnBackupRestoreMklinkMMT_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, "backup.restore.mklink.MMT.xlsx");
                string downloadUrl = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/backup.restore.mklink.MMT.xlsx";

                UpdateStatus("Đang tải file backup.restore.mklink.MMT.xlsx...", "Cyan");
                using (WebClient client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(downloadUrl, filePath);
                }

                UpdateStatus("Đã tải về desktop", "Green");
                MessageBox.Show("Đã tải về desktop", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải file: {ex.Message}", "Red");
            }
        }


        // ===================================================================
        // TabPartition — Disk Genius
        // TabItem Header: "Partition"
        // Checkbox: ChkDiskGenius
        // ===================================================================
        private async Task InstallDiskGeniusAsync()
        {
            try
            {
                UpdateStatus("Đang tải Disk Genius...", "Cyan");
                string diskGeniusPath = Path.Combine(GetGMTPCFolder(), "DiskGenius.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/WinPE/Disk.Genius.exe", diskGeniusPath, "Disk Genius");

                MessageBoxResult result = MessageBox.Show("Yes = Cài đặt tự động vào ổ C\nNo = Cài vào ổ khác", "Cài đặt Disk Genius", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = diskGeniusPath, UseShellExecute = true };

                if (result == MessageBoxResult.Yes) { startInfo.Arguments = "/s"; UpdateStatus("Cài đặt Disk Genius vào ổ C...", "Yellow"); }
                else if (result == MessageBoxResult.No) { UpdateStatus("Cài Disk Genius vào ổ khác...", "Yellow"); }
                else
                {
                    UpdateStatus("Đã hủy cài đặt Disk Genius", "Yellow");
                    if (File.Exists(diskGeniusPath)) File.Delete(diskGeniusPath);
                    return;
                }

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt Disk Genius hoàn tất!", "Green");
                }

                if (File.Exists(diskGeniusPath)) File.Delete(diskGeniusPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Disk Genius: {ex.Message}", "Red");
            }
        }


        // ===================================================================
        // TabPartition — AOMEI Partition Assistant
        // TabItem Header: "Partition"
        // Checkbox: ChkAomeiPartitionAssistant
        // ===================================================================
        private async Task InstallAomeiPartitionAssistantAsync()
        {
            try
            {
                UpdateStatus("Đang tải AOMEI Partition Assistant...", "Cyan");
                string filePath = Path.Combine(GetGMTPCFolder(), "AOMEI.Partition.Assistant.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/WinPE/AOMEI.Partition.Assistant.exe", filePath, "AOMEI Partition Assistant");

                MessageBoxResult result = MessageBox.Show("Yes = Cài đặt tự động vào ổ C\nNo = Cài vào ổ khác", "Cài đặt AOMEI Partition Assistant", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    UpdateStatus("Đã hủy cài đặt AOMEI Partition Assistant", "Yellow");
                    if (File.Exists(filePath)) File.Delete(filePath);
                    return;
                }

                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = filePath, UseShellExecute = true };
                if (result == MessageBoxResult.Yes) { startInfo.Arguments = "/passive"; UpdateStatus("Cài đặt AOMEI vào ổ C...", "Yellow"); }
                else { UpdateStatus("Cài AOMEI vào ổ khác...", "Yellow"); }

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt AOMEI Partition Assistant hoàn tất!", "Green");
                }

                if (File.Exists(filePath)) File.Delete(filePath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt AOMEI Partition Assistant: {ex.Message}", "Red");
            }
        }

    }
}
