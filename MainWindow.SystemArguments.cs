// =======================================================================
// AI Summary: 2026-03-14 - Updated to use DownloadSingleLinkFastAsync (16 segments) for all GitHub links
// =======================================================================
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
 * Date: 2026-03-11
 * - Added GHOST_OF_TSUSHIMA_PART01_URL to PART29_URL constants for Ghost of Tsushima
 * Date: 2026-03-09
 * - Added ChkAdvancedCodecPack, ChkTeraCopy, ChkVPN1111 constants and methods
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
        //             ChkDirectX, ChkJava, ChkOpenAL, ChkRevoUninstaller
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

        // Revo Uninstaller
        private const string REVO_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/RevoUninstallerPro.exe";
        private const string REVO_INSTALL_ARGUMENTS = "/S";

        // Zalo
        private const string ZALO_DOWNLOAD_URL = "https://zalo.me/download/zalo-pc?utm=90000";
        private const string ZALO_INSTALL_ARGUMENTS = "/s";

        // ===================================================================
        // TabGaming — Ghost of Tsushima
        // TabItem Header: "Gaming"
        // Checkbox: ChkGhostOfTsushima
        // ===================================================================
        // Ghost of Tsushima (29 parts)
        private const string GHOST_OF_TSUSHIMA_PART01_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part01.exe";
        private const string GHOST_OF_TSUSHIMA_PART02_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part02.rar";
        private const string GHOST_OF_TSUSHIMA_PART03_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part03.rar";
        private const string GHOST_OF_TSUSHIMA_PART04_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part04.rar";
        private const string GHOST_OF_TSUSHIMA_PART05_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part05.rar";
        private const string GHOST_OF_TSUSHIMA_PART06_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part06.rar";
        private const string GHOST_OF_TSUSHIMA_PART07_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part07.rar";
        private const string GHOST_OF_TSUSHIMA_PART08_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part08.rar";
        private const string GHOST_OF_TSUSHIMA_PART09_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part09.rar";
        private const string GHOST_OF_TSUSHIMA_PART10_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part10.rar";
        private const string GHOST_OF_TSUSHIMA_PART11_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part11.rar";
        private const string GHOST_OF_TSUSHIMA_PART12_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part12.rar";
        private const string GHOST_OF_TSUSHIMA_PART13_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part13.rar";
        private const string GHOST_OF_TSUSHIMA_PART14_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part14.rar";
        private const string GHOST_OF_TSUSHIMA_PART15_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part15.rar";
        private const string GHOST_OF_TSUSHIMA_PART16_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part16.rar";
        private const string GHOST_OF_TSUSHIMA_PART17_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part17.rar";
        private const string GHOST_OF_TSUSHIMA_PART18_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part18.rar";
        private const string GHOST_OF_TSUSHIMA_PART19_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part19.rar";
        private const string GHOST_OF_TSUSHIMA_PART20_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part20.rar";
        private const string GHOST_OF_TSUSHIMA_PART21_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part21.rar";
        private const string GHOST_OF_TSUSHIMA_PART22_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part22.rar";
        private const string GHOST_OF_TSUSHIMA_PART23_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part23.rar";
        private const string GHOST_OF_TSUSHIMA_PART24_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part24.rar";
        private const string GHOST_OF_TSUSHIMA_PART25_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part25.rar";
        private const string GHOST_OF_TSUSHIMA_PART26_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part26.rar";
        private const string GHOST_OF_TSUSHIMA_PART27_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part27.rar";
        private const string GHOST_OF_TSUSHIMA_PART28_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part28.rar";
        private const string GHOST_OF_TSUSHIMA_PART29_URL = "https://github.com/ghostminhtoan/MMT/releases/download/game/Ghost.of.Tsushima_LinkNeverDie.Com.part29.rar";

        // ===================================================================
        // TabOffice — Links (B) and Arguments (C)
        // TabItem Header: "Office"
        // Checkboxes: ChkActivateOffice, ChkOfficeToolPlus, ChkOfficeSoftmaker,
        //             ChkGouenjiFonts, ChkNotepadPlusPlus
        // ===================================================================
        // Gouenji Fansub Fonts (Tab: Office)
        private const string GOUENJI_FONTS_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Gouenji.Fansub.Fonts.exe";
        private const string GOUENJI_FONTS_INSTALL_ARGUMENTS = "/passive /norestart";

        // Notepad++ (Tab: Office)
        private const string NOTEPAD_PLUS_PLUS_DOWNLOAD_URL = "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.9.2/npp.8.9.2.Installer.x64.msi";
        private const string NOTEPAD_PLUS_PLUS_INSTALL_ARGUMENTS = "/passive /norestart";

        // ===================================================================
        // TabDriver — Links (B) and Arguments (C)
        // TabItem Header: "Driver"
        // Checkboxes: Chk3DPChip, Chk3DPNet
        // ===================================================================
        // 3DP Chip (Tab: Driver)
        private const string DPCHIP_DOWNLOAD_URL = "https://www.3dpchip.com/3dp/chip.exe";
        private const string DPCHIP_INSTALL_ARGUMENTS = "/S";

        // 3DP Net (Tab: Driver)
        private const string DPNET_DOWNLOAD_URL = "https://www.3dpchip.com/3dp/net.exe";
        private const string DPNET_INSTALL_ARGUMENTS = "/S";

        // ===================================================================
        // TabBrowser — Links (B) and Arguments (C)
        // TabItem Header: "Browser"
        // Checkboxes: ChkChrome, ChkCocCoc, ChkEdge
        // ===================================================================
        // Chrome (Tab: Browser)
        private const string CHROME_DOWNLOAD_URL = "https://dl.google.com/chrome/install/latest/chrome_installer.exe";
        private const string CHROME_INSTALL_ARGUMENTS = "/silent /install";

        // CocCoc (Tab: Browser)
        private const string COCCOC_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/CocCoc.exe";
        private const string COCCOC_INSTALL_ARGUMENTS = "/silent /install";

        // Edge (Tab: Browser)
        private const string EDGE_DOWNLOAD_URL = "https://go.microsoft.com/fwlink/?linkid=2108834&Channel=Stable&language=vi";
        private const string EDGE_INSTALL_ARGUMENTS = "/silent /install";

        // ===================================================================
        // TabMultimedia — Advanced Codec Pack
        // TabItem Header: "Multimedia"
        // Checkboxes: ChkAdvancedCodecPack
        // ===================================================================
        private const string ADVANCEDCODECPACK_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/ADVANCED_Codec_Pack.exe";
        private const string ADVANCEDCODECPACK_INSTALL_ARGUMENTS = "/S /V/qn";

        // ===================================================================
        // TabSystem — PowerISO
        // TabItem Header: "System"
        // Checkboxes: ChkPowerISO, ChkTeraCopy, ChkVPN1111
        // ===================================================================
        private const string POWERISO_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/PowerISO.exe";
        private const string POWERISO_INSTALL_ARGUMENTS = "/S";

        // TeraCopy
        private const string TERACOPY_DOWNLOAD_URL = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/TeraCopy.Pro.v3.17.0.0.exe";
        private const string TERACOPY_INSTALL_ARGUMENTS = "/S";

        // VPN 1111 (Cloudflare)
        private const string VPN1111_DOWNLOAD_URL = "https://1111-releases.cloudflareclient.com/win/latest";
        private const string VPN1111_INSTALL_ARGUMENTS = "/passive";

        private async Task InstallZaloAsync()
        {
            try
            {
                // Get isolated download folder: {Drive}:\temp\Zalo\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("Zalo");

                UpdateStatus("Đang tải Zalo...", "Cyan");
                string zaloPath = Path.Combine(taskFolder, "ZaloSetup.exe");
                await DownloadSingleLinkFastAsync(ZALO_DOWNLOAD_URL, zaloPath, "Zalo");

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
                // Get isolated download folder: {Drive}:\temp\PowerISO\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("PowerISO");

                UpdateStatus("Đang tải PowerISO...", "Cyan");
                string powerISOPath = Path.Combine(taskFolder, "PowerISO.exe");
                await DownloadSingleLinkFastAsync(POWERISO_DOWNLOAD_URL, powerISOPath, "PowerISO");

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
                    Arguments = POWERISO_INSTALL_ARGUMENTS,
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
        // TabSystem — Google Drive
        // TabItem Header: "System"
        // Checkbox: ChkGoogleDrive
        // ===================================================================
        private async Task InstallGoogleDriveAsync()
        {
            try
            {
                // Get isolated download folder: {Drive}:\temp\GoogleDrive\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("GoogleDrive");
                
                UpdateStatus("Đang tải Google Drive...", "Cyan");
                string googleDrivePath = Path.Combine(taskFolder, "GoogleDriveSetup.exe");
                await DownloadSingleLinkFastAsync("https://dl.google.com/drive-file-stream/GoogleDriveSetup.exe", googleDrivePath, "Google Drive");

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
                // Get isolated download folder: {Drive}:\temp\FolderSize\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("FolderSize");

                UpdateStatus("Đang tải FolderSize...", "Cyan");
                string folderSizePath = Path.Combine(taskFolder, "FolderSize-2.6-x64.msi");
                await DownloadSingleLinkFastAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/FolderSize-2.6-x64.msi", folderSizePath, "FolderSize");

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
                             // Thêm checkbox cho Gouenji Fonts
                             ChkGouenjiFonts.IsChecked == true ||
                             // Thêm checkbox cho Notepad++
                             ChkNotepadPlusPlus.IsChecked == true ||
                             // Thêm checkbox cho AOMEI Partition Assistant
                             ChkAomeiPartitionAssistant.IsChecked == true ||
                             // Thêm checkbox cho PowerISO
                             ChkPowerISO.IsChecked == true ||
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
                             ChkGhostOfTsushima.IsChecked == true ||
                             ChkUltraviewer.IsChecked == true ||
                             // Multimedia
                             ChkPotPlayer.IsChecked == true ||
                             ChkFastStone.IsChecked == true ||
                             ChkFoxit.IsChecked == true ||
                             ChkBandiview.IsChecked == true ||
                             ChkTeamViewerQS.IsChecked == true ||
                             ChkTeamViewerFull.IsChecked == true ||
                             ChkAnyDesk.IsChecked == true ||
                             ChkVMWare162Lite.IsChecked == true ||
                             // Windows - Microsoft
                             ChkWin11_26H1.IsChecked == true ||
                             ChkWin10LtscIot21H2.IsChecked == true ||
                             // Multimedia - Advanced Codec Pack
                             ChkAdvancedCodecPack.IsChecked == true ||
                             // System - TeraCopy and VPN 1111
                             ChkTeraCopy.IsChecked == true ||
                             ChkVPN1111.IsChecked == true;

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
                // Get isolated download folder: {Drive}:\temp\DISM++\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("DISM++");
                
                UpdateStatus("Đang tải DISM++...", "Cyan");
                string dismppPath = Path.Combine(taskFolder, "DISM++.exe");
                await DownloadSingleLinkFastAsync("https://github.com/ghostminhtoan/MMT/releases/download/WinPE/DISM++.exe", dismppPath, "DISM++ Installer");

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
                // Get isolated download folder: {Drive}:\temp\NetLimiter\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("NetLimiter");
                
                UpdateStatus("Đang tải NetLimiter...", "Cyan");
                string netLimiterPath = Path.Combine(taskFolder, "NetLimiter.exe");
                await DownloadSingleLinkFastAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/netlimiter-4.1.12.0.exe", netLimiterPath, "NetLimiter");

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
                // Get isolated download folder: {Drive}:\temp\ComfortClipboardPro\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("ComfortClipboardPro");
                
                UpdateStatus("Đang tải Comfort Clipboard Pro...", "Cyan");
                string comfortClipboardPath = Path.Combine(taskFolder, "Comfort.Clipboard.Pro.7.0.2.exe");
                await DownloadSingleLinkFastAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Comfort.Clipboard.Pro.7.0.2.exe", comfortClipboardPath, "Comfort Clipboard Pro Installer");

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
                // Get isolated download folder: {Drive}:\temp\MMTApps\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("MMTApps");
                
                UpdateStatus("Đang tải MMT.Apps.exe...", "Cyan");
                string mmtAppsPath = Path.Combine(taskFolder, "MMT.Apps.exe");
                await DownloadSingleLinkFastAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/MMT.Apps.exe", mmtAppsPath, "MMT Apps Installer");

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
                // Get isolated download folder: {Drive}:\temp\DiskGenius\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("DiskGenius");
                
                UpdateStatus("Đang tải Disk Genius...", "Cyan");
                string diskGeniusPath = Path.Combine(taskFolder, "DiskGenius.exe");
                await DownloadSingleLinkFastAsync("https://github.com/ghostminhtoan/MMT/releases/download/WinPE/Disk.Genius.exe", diskGeniusPath, "Disk Genius");

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
                // Get isolated download folder: {Drive}:\temp\AOMEI\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("AOMEI");
                
                UpdateStatus("Đang tải AOMEI Partition Assistant...", "Cyan");
                string filePath = Path.Combine(taskFolder, "AOMEI.Partition.Assistant.exe");
                await DownloadSingleLinkFastAsync("https://github.com/ghostminhtoan/MMT/releases/download/WinPE/AOMEI.Partition.Assistant.exe", filePath, "AOMEI Partition Assistant");

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

        // ===================================================================
        // TabMultimedia — Advanced Codec Pack
        // TabItem Header: "Multimedia"
        // Checkbox: ChkAdvancedCodecPack
        // ===================================================================
        private async Task InstallAdvancedCodecPackAsync()
        {
            try
            {
                // Get isolated download folder: {Drive}:\temp\AdvancedCodecPack\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("AdvancedCodecPack");
                
                UpdateStatus("Đang tải Advanced Codec Pack...", "Cyan");
                string codecPath = Path.Combine(taskFolder, "ADVANCED_Codec_Pack.exe");
                await DownloadSingleLinkFastAsync(ADVANCEDCODECPACK_DOWNLOAD_URL, codecPath, "Advanced Codec Pack");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt Advanced Codec Pack (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = codecPath,
                    Arguments = ADVANCEDCODECPACK_INSTALL_ARGUMENTS,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Advanced Codec Pack đã hoàn tất.", "Green");
                }

                if (File.Exists(codecPath)) File.Delete(codecPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài Advanced Codec Pack: {ex.Message}", "Red");
            }
        }

        // ===================================================================
        // TabSystem — TeraCopy
        // TabItem Header: "System"
        // Checkbox: ChkTeraCopy
        // ===================================================================
        private async Task InstallTeraCopyAsync()
        {
            try
            {
                // Get isolated download folder: {Drive}:\temp\TeraCopy\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("TeraCopy");
                
                UpdateStatus("Đang tải TeraCopy...", "Cyan");
                string teraPath = Path.Combine(taskFolder, "TeraCopy.Pro.v3.17.0.0.exe");
                await DownloadSingleLinkFastAsync(TERACOPY_DOWNLOAD_URL, teraPath, "TeraCopy");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt TeraCopy (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = teraPath,
                    Arguments = TERACOPY_INSTALL_ARGUMENTS,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("TeraCopy đã hoàn tất.", "Green");
                }

                if (File.Exists(teraPath)) File.Delete(teraPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài TeraCopy: {ex.Message}", "Red");
            }
        }

        // ===================================================================
        // TabSystem — VPN 1111 (Cloudflare)
        // TabItem Header: "System"
        // Checkbox: ChkVPN1111
        // ===================================================================
        private async Task InstallVPN1111Async()
        {
            try
            {
                // Get isolated download folder: {Drive}:\temp\VPN1111\
                string taskFolder = DownloadConfiguration.GetTaskDownloadFolder("VPN1111");
                
                UpdateStatus("Đang tải VPN 1111...", "Cyan");
                string vpnPath = Path.Combine(taskFolder, "VPN1111.msi");
                await DownloadSingleLinkFastAsync(VPN1111_DOWNLOAD_URL, vpnPath, "VPN 1111");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt VPN 1111 (passive)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "msiexec.exe",
                    Arguments = $"/i \"{vpnPath}\" /passive",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("VPN 1111 đã hoàn tất.", "Green");
                }

                if (File.Exists(vpnPath)) File.Delete(vpnPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài VPN 1111: {ex.Message}", "Red");
            }
        }

    }
}
