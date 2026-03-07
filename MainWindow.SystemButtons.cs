// =======================================================================
// MainWindow.SystemButtons.cs
// Chức năng: Logic các nút điều khiển chung: Select All, Select None,
//            SelectNoneAllTabs, Install, Pause, Resume, Refresh Color,
//            BtnDownloadPage, DPI controls
// Cập nhật gần đây:
//   - 2026-03-05: Thêm currentDPIScale, DPI_STEPS, ApplyDPIScale từ xaml.cs
//                 theo AI_WORKFLOW.md
// =======================================================================
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
using System.Windows.Media.Animation;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // ===================== DPI Scale Fields =====================
        private double currentDPIScale = 1.0;
        private readonly int[] DPI_STEPS = new int[] { 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 200 };

        private void ApplyDPIScale()
        {
            ScaleTransform scaleTransform = new ScaleTransform(currentDPIScale, currentDPIScale);
            MainGrid.LayoutTransform = scaleTransform;

            bool isPortrait = SystemParameters.PrimaryScreenWidth < SystemParameters.PrimaryScreenHeight;
            double designMaxWidth  = isPortrait ? 580  : 1000;
            double designMaxHeight = isPortrait ? 950  : 750;

            var workArea = SystemParameters.WorkArea;
            this.MaxHeight = Math.Min(designMaxHeight * currentDPIScale, workArea.Height);
            this.MaxWidth  = Math.Min(designMaxWidth  * currentDPIScale, workArea.Width);

            MainGrid.InvalidateMeasure();
            this.InvalidateMeasure();

            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
            {
                this.SizeToContent = SizeToContent.Manual;
                this.Width  = double.NaN;
                this.Height = double.NaN;
                this.SizeToContent = SizeToContent.WidthAndHeight;
            }));

            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                const double margin = 12.0;
                try
                {
                    double maxAllowedHeight = Math.Max(0, workArea.Height - margin);
                    double maxAllowedWidth  = Math.Max(0, workArea.Width  - margin);

                    if (this.ActualHeight > maxAllowedHeight) this.Height = maxAllowedHeight;
                    if (this.ActualWidth  > maxAllowedWidth)  this.Width  = maxAllowedWidth;

                    if (this.Top  < workArea.Top  + margin) this.Top  = workArea.Top  + margin;
                    if (this.Left < workArea.Left + margin) this.Left = workArea.Left + margin;
                    if (this.Top  + this.Height > workArea.Bottom - margin) this.Top  = workArea.Bottom - margin - this.Height;
                    if (this.Left + this.Width  > workArea.Right  - margin) this.Left = workArea.Right  - margin - this.Width;
                }
                catch { }

                try { this.SizeToContent = SizeToContent.Manual; } catch { }
            }));

            int dpiPercent = (int)(currentDPIScale * 100);
            string dpiText = $"{dpiPercent}%";

            ComboBoxItem selectedItem = CboDPIValue.SelectedItem as ComboBoxItem;
            string currentSelection  = selectedItem?.Content.ToString() ?? "";

            if (currentSelection != dpiText)
            {
                foreach (ComboBoxItem item in CboDPIValue.Items)
                {
                    if (item.Content.ToString() == dpiText)
                    {
                        CboDPIValue.SelectedItem = item;
                        break;
                    }
                }
            }

            UpdateStatus($"Đã đặt tỷ lệ DPI: {dpiText}", "Green");
        }

        private void ResetDPIButtonStates() { /* Kept for compatibility */ }
        private void BtnDPIMinus_Click(object sender, RoutedEventArgs e)
        {
            // Find current index in DPI_STEPS
            int currentPercent = (int)Math.Round(currentDPIScale * 100.0);
            int idx = Array.IndexOf(DPI_STEPS, currentPercent);
            if (idx <= 0) idx = 0; else idx--;
            currentDPIScale = DPI_STEPS[idx] / 100.0;
            // Update combo box selection if available
            try { if (CboDPIValue != null && idx >= 0 && idx < CboDPIValue.Items.Count) CboDPIValue.SelectedIndex = idx; } catch { }
            ApplyDPIScale();
            UpdateSecondaryStatus($"Đã đổi DPI: {DPI_STEPS[idx]}%", "Cyan");
        }

        private void BtnDPIPlus_Click(object sender, RoutedEventArgs e)
        {
            int currentPercent = (int)Math.Round(currentDPIScale * 100.0);
            int idx = Array.IndexOf(DPI_STEPS, currentPercent);
            if (idx < 0) // if current percent not exactly in steps, find nearest
            {
                // Find closest step
                int closest = 0; int minDiff = int.MaxValue;
                for (int i = 0; i < DPI_STEPS.Length; i++)
                {
                    int diff = Math.Abs(DPI_STEPS[i] - currentPercent);
                    if (diff < minDiff) { minDiff = diff; closest = i; }
                }
                idx = closest;
            }
            if (idx < DPI_STEPS.Length - 1) idx++; else idx = DPI_STEPS.Length - 1;
            currentDPIScale = DPI_STEPS[idx] / 100.0;
            try { if (CboDPIValue != null && idx >= 0 && idx < CboDPIValue.Items.Count) CboDPIValue.SelectedIndex = idx; } catch { }
            ApplyDPIScale();
            UpdateSecondaryStatus($"Đã đổi DPI: {DPI_STEPS[idx]}%", "Cyan");
        }

        private void CboDPIValue_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboDPIValue.SelectedItem == null) return;

            // Get the ComboBoxItem and extract its Content
            ComboBoxItem selectedItem = CboDPIValue.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;

            string selectedValue = selectedItem.Content.ToString();

            // Parse the percentage and convert to decimal
            if (selectedValue.EndsWith("%"))
            {
                selectedValue = selectedValue.Substring(0, selectedValue.Length - 1);
            }

            if (double.TryParse(selectedValue, out double percent))
            {
                // Ensure the selected percent is among our allowed steps. If not, snap to the nearest.
                int sel = (int)Math.Round(percent);
                int closest = 0; int minDiff = int.MaxValue;
                for (int i = 0; i < DPI_STEPS.Length; i++)
                {
                    int diff = Math.Abs(DPI_STEPS[i] - sel);
                    if (diff < minDiff) { minDiff = diff; closest = i; }
                }
                currentDPIScale = DPI_STEPS[closest] / 100.0;
                // Keep combobox selection consistent
                try { if (CboDPIValue != null && closest >= 0 && closest < CboDPIValue.Items.Count) CboDPIValue.SelectedIndex = closest; } catch { }
                ApplyDPIScale();
                UpdateSecondaryStatus($"Đã đổi DPI: {DPI_STEPS[closest]}%", "Cyan");
            }
        }

        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://tinyurl.com/gmtpcdonate");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi mở liên kết ủng hộ: {ex.Message}", "Red");
            }
        }

        // ===================== Event Handlers for New Buttons =====================
        private void BtnSelectAll_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đã chọn tất cả trong tab hiện tại", "Green");

            // Lấy tab hiện tại đang được chọn
            if (MainTabControl.SelectedItem is TabItem selectedTab)
            {
                string tabHeader = selectedTab.Header.ToString();

                // Nếu tab là "Popular"
                if (tabHeader == "Popular")
                {
                    // Chọn tất cả các checkbox trong tab Popular
                    ChkInstallIDM.IsChecked = true;
                    ChkActivateWindows.IsChecked = true;
                    ChkPauseWindowsUpdate.IsChecked = true;
                    ChkInstallWinRAR.IsChecked = true;
                    ChkInstallBID.IsChecked = true;
                    ChkVcredist.IsChecked = true;
                    ChkDirectX.IsChecked = true;
                    ChkJava.IsChecked = true;
                    ChkOpenAL.IsChecked = true;
                    Chk3DPChip.IsChecked = true;
                    Chk3DPNet.IsChecked = true;
                    ChkRevoUninstaller.IsChecked = true;
                }
                // Nếu tab là "System"
                else if (tabHeader == "System")
                {
                    // Chọn checkbox trong tab System
                    ChkMMTApps.IsChecked = true;
                    ChkDISMPP.IsChecked = true;
                    ChkComfortClipboardPro.IsChecked = true;
                    ChkFolderSize.IsChecked = true;
                    ChkPowerISO.IsChecked = true;
                    ChkVPN1111.IsChecked = true;
                    ChkTeracopy.IsChecked = true;
                    ChkGoogleDrive.IsChecked = true;
                    ChkNetLimiter.IsChecked = true;
                }
                else if (tabHeader == "Office")
                {
                    ChkOfficeToolPlus.IsChecked = true;
                    ChkOfficeSoftmaker.IsChecked = true;
                    ChkActivateOffice.IsChecked = true;
                    ChkFonts.IsChecked = true;
                    ChkNotepadPP.IsChecked = true;
                }
                // Nếu tab là "Partition"
                else if (tabHeader == "Partition")
                {
                    // Chọn checkbox trong tab Partition
                    ChkAomeiPartitionAssistant.IsChecked = true;
                    ChkDiskGenius.IsChecked = true;
                }
                // Nếu tab là "Gaming"
                else if (tabHeader == "Gaming")
                {
                    // Chọn checkbox trong tab Gaming
                    ChkProcessLasso.IsChecked = true;
                    ChkThrottlestop.IsChecked = true;
                    ChkMSIAfterburner.IsChecked = true;
                    ChkLeagueOfLegends.IsChecked = true;
                    ChkPorofessor.IsChecked = true;
                    ChkSamuraiMaiden.IsChecked = true;
                }
                // Nếu tab là "Browser"
                else if (tabHeader == "Browser")
                {
                    ChkChrome.IsChecked = true;
                    ChkCocCoc.IsChecked = true;
                    ChkEdge.IsChecked = true;
                }
                else if (tabHeader == "Multimedia")
                {
                    // Chọn checkbox trong tab Multimedia
                    ChkPotPlayer.IsChecked = true;
                    ChkFastStone.IsChecked = true;
                    ChkFoxit.IsChecked = true;
                    ChkBandiview.IsChecked = true;
                    ChkAdvancedCodec.IsChecked = true;
                }
                // Nếu tab là "Remote Desktop"
                else if (tabHeader == "Remote Desktop")
                {
                    // Chọn checkbox trong tab Remote Desktop
                    ChkUltraviewer.IsChecked = true;
                    ChkTeamViewerQS.IsChecked = true;
                    ChkTeamViewerFull.IsChecked = true;
                    ChkAnyDesk.IsChecked = true;
                    ChkVMWare162Lite.IsChecked = true;
                }
                else if (tabHeader == "Windows - Microsoft")
                {
                    ChkWin11_26H1.IsChecked = true;
                    ChkWin10_20H2_2022April.IsChecked = true;
                }
                else if (tabHeader == "Windows Mod MMT")
                {
                    ChkWin10LtscIot21H2.IsChecked = true;
                }
            }

            UpdateInstallButtonState();
        }

        private void BtnSelectNone_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đã hủy chọn tất cả trong tab hiện tại", "Yellow");

            // Lấy tab hiện tại đang được chọn
            if (MainTabControl.SelectedItem is TabItem selectedTab)
            {
                string tabHeader = selectedTab.Header.ToString();

                // Nếu tab là "Popular"
                if (tabHeader == "Popular")
                {
                    // Bỏ chọn tất cả các checkbox trong tab Popular
                    ChkInstallIDM.IsChecked = false;
                    ChkActivateWindows.IsChecked = false;
                    ChkPauseWindowsUpdate.IsChecked = false;
                    ChkInstallWinRAR.IsChecked = false;
                    ChkInstallBID.IsChecked = false;
                    ChkVcredist.IsChecked = false;
                    ChkDirectX.IsChecked = false;
                    ChkJava.IsChecked = false;
                    ChkOpenAL.IsChecked = false;
                    Chk3DPChip.IsChecked = false;
                    Chk3DPNet.IsChecked = false;
                    ChkRevoUninstaller.IsChecked = false;
                }
                // Nếu tab là "System"
                else if (tabHeader == "System")
                {
                    // Bỏ chọn checkbox trong tab System
                    ChkMMTApps.IsChecked = false;
                    ChkDISMPP.IsChecked = false;
                    ChkComfortClipboardPro.IsChecked = false;
                    ChkFolderSize.IsChecked = false;
                    ChkPowerISO.IsChecked = false;
                    ChkVPN1111.IsChecked = false;
                    ChkTeracopy.IsChecked = false;
                    ChkGoogleDrive.IsChecked = false;
                    ChkNetLimiter.IsChecked = false;
                }
                else if (tabHeader == "Office")
                {
                    ChkOfficeToolPlus.IsChecked = false;
                    ChkOfficeSoftmaker.IsChecked = false;
                    ChkActivateOffice.IsChecked = false;
                    ChkFonts.IsChecked = false;
                    ChkNotepadPP.IsChecked = false;
                }
                // Nếu tab là "Partition"
                else if (tabHeader == "Partition")
                {
                    // Bỏ chọn checkbox trong tab Partition
                    ChkAomeiPartitionAssistant.IsChecked = false;
                    ChkDiskGenius.IsChecked = false;
                }
                // Nếu tab là "Gaming"
                else if (tabHeader == "Gaming")
                {
                    // Bỏ chọn checkbox trong tab Gaming
                    ChkProcessLasso.IsChecked = false;
                    ChkThrottlestop.IsChecked = false;
                    ChkMSIAfterburner.IsChecked = false;
                    ChkLeagueOfLegends.IsChecked = false;
                    ChkPorofessor.IsChecked = false;
                    ChkSamuraiMaiden.IsChecked = false;
                }
                // Nếu tab là "Browser"
                else if (tabHeader == "Browser")
                {
                    ChkChrome.IsChecked = false;
                    ChkCocCoc.IsChecked = false;
                    ChkEdge.IsChecked = false;
                }
                else if (tabHeader == "Multimedia")
                {
                    // Bỏ chọn checkbox trong tab Multimedia
                    ChkPotPlayer.IsChecked = false;
                    ChkFastStone.IsChecked = false;
                    ChkFoxit.IsChecked = false;
                    ChkBandiview.IsChecked = false;
                    ChkAdvancedCodec.IsChecked = false;
                }
                // Nếu tab là "Remote Desktop"
                else if (tabHeader == "Remote Desktop")
                {
                    // Bỏ chọn checkbox trong tab Remote Desktop
                    ChkUltraviewer.IsChecked = false;
                    ChkTeamViewerQS.IsChecked = false;
                    ChkTeamViewerFull.IsChecked = false;
                    ChkAnyDesk.IsChecked = false;
                    ChkVMWare162Lite.IsChecked = false;
                }
                else if (tabHeader == "Windows - Microsoft")
                {
                    ChkWin11_26H1.IsChecked = false;
                    ChkWin10_20H2_2022April.IsChecked = false;
                }
                else if (tabHeader == "Windows Mod MMT")
                {
                    ChkWin10LtscIot21H2.IsChecked = false;
                }
            }

            UpdateInstallButtonState();
        }

        private void BtnSelectNoneAllTabs_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đã hủy chọn tất cả trong tất cả các tab", "Yellow");

            // Bỏ chọn tất cả các checkbox trong tab Popular
            ChkInstallIDM.IsChecked = false;
            ChkActivateWindows.IsChecked = false;
            ChkActivateOffice.IsChecked = false;
            ChkPauseWindowsUpdate.IsChecked = false;
            ChkInstallWinRAR.IsChecked = false;
            ChkInstallBID.IsChecked = false;
            ChkVcredist.IsChecked = false;
            ChkDirectX.IsChecked = false;
            ChkJava.IsChecked = false;
            ChkOpenAL.IsChecked = false;
            Chk3DPChip.IsChecked = false;
            Chk3DPNet.IsChecked = false;
            ChkChrome.IsChecked = false;
            ChkCocCoc.IsChecked = false;
            ChkEdge.IsChecked = false;
            ChkPotPlayer.IsChecked = false;
            ChkFastStone.IsChecked = false;
            ChkFoxit.IsChecked = false;
            ChkBandiview.IsChecked = false;
            ChkRevoUninstaller.IsChecked = false;

            // Bỏ chọn checkbox trong tab System
            ChkMMTApps.IsChecked = false;
            ChkDISMPP.IsChecked = false;
            ChkComfortClipboardPro.IsChecked = false;
            ChkFolderSize.IsChecked = false;
            ChkPowerISO.IsChecked = false;
            ChkVPN1111.IsChecked = false;
            ChkTeracopy.IsChecked = false;
            ChkGoogleDrive.IsChecked = false;
            ChkNetLimiter.IsChecked = false;

            // Bỏ chọn checkbox trong tab Partition
            ChkAomeiPartitionAssistant.IsChecked = false;
            ChkDiskGenius.IsChecked = false;

            // Bỏ chọn checkbox trong tab Gaming
            ChkProcessLasso.IsChecked = false;
            ChkThrottlestop.IsChecked = false;
            ChkMSIAfterburner.IsChecked = false;
            ChkLeagueOfLegends.IsChecked = false;
            ChkPorofessor.IsChecked = false;
            ChkSamuraiMaiden.IsChecked = false;

            // Bỏ chọn checkbox trong tab Remote Desktop
            ChkUltraviewer.IsChecked = false;
            ChkTeamViewerQS.IsChecked = false;
            ChkTeamViewerFull.IsChecked = false;
            ChkAnyDesk.IsChecked = false;
            ChkVMWare162Lite.IsChecked = false;

            // Bỏ chọn checkbox trong tab Office
            ChkOfficeToolPlus.IsChecked = false;
            ChkOfficeSoftmaker.IsChecked = false;
            ChkFonts.IsChecked = false;

            // Bỏ chọn Notepad++
            ChkNotepadPP.IsChecked = false;

            // Bỏ chọn Advanced Codec
            ChkAdvancedCodec.IsChecked = false;

            // Bỏ chọn Win 11
            ChkWin11_26H1.IsChecked = false;

            // Bỏ chọn Win 10 20H2 April 2022
            ChkWin10_20H2_2022April.IsChecked = false;

            UpdateInstallButtonState();
        }

        private async void BtnInstall_Click(object sender, RoutedEventArgs e)
        {
            // Đặt trạng thái đang cài đặt
            SetInstallingState(true);
            
            UpdateStatus("Đang chờ...", "Yellow"); // Thêm phản hồi tức thì
            await Task.Delay(1); // Cho phép UI cập nhật ngay lập tức

            _cancellationTokenSource = new CancellationTokenSource();
            _pauseCts = new CancellationTokenSource();
            _pauseEvent.Set(); // Mặc định là không pause
            BtnPause.Content = "Pause";
            BtnStop.IsEnabled = true;
            BtnPause.IsEnabled = true;
            BtnInstall.IsEnabled = false;

            var tasks = new List<(Func<Task> Action, CheckBox CheckBox)>();

            if (ChkInstallIDM.IsChecked == true) tasks.Add((() => RunAutomatedProcessAsync(), ChkInstallIDM));
            if (ChkActivateWindows.IsChecked == true) tasks.Add((() => Task.Run(() => ActivateWindows()), ChkActivateWindows));
            if (ChkActivateOffice.IsChecked == true) tasks.Add((() => Task.Run(() => ActivateOffice()), ChkActivateOffice));
            if (ChkOfficeToolPlus.IsChecked == true) tasks.Add((InstallOfficeToolPlusAsync, ChkOfficeToolPlus)); // Thêm task cho Office Tool Plus
            if (ChkPauseWindowsUpdate.IsChecked == true) tasks.Add((() => Task.Run(() => PauseWindowsUpdate()), ChkPauseWindowsUpdate));
            if (ChkInstallWinRAR.IsChecked == true) tasks.Add((InstallAndActivateWinRARAsync, ChkInstallWinRAR));
            if (ChkInstallBID.IsChecked == true) tasks.Add((InstallAndActivateBIDAsync, ChkInstallBID));
            if (ChkVcredist.IsChecked == true) tasks.Add((InstallVcredistAsync, ChkVcredist));
            if (ChkDirectX.IsChecked == true) tasks.Add((InstallDirectXAsync, ChkDirectX));
            if (ChkJava.IsChecked == true) tasks.Add((InstallJavaAsync, ChkJava));
            if (ChkOpenAL.IsChecked == true) tasks.Add((InstallOpenALAsync, ChkOpenAL));
            if (ChkChrome.IsChecked == true) tasks.Add((InstallChromeAsync, ChkChrome));
            if (ChkCocCoc.IsChecked == true) tasks.Add((InstallCocCocAsync, ChkCocCoc));
            if (ChkEdge.IsChecked == true) tasks.Add((InstallEdgeAsync, ChkEdge));
            if (ChkPotPlayer.IsChecked == true) tasks.Add((InstallPotPlayerAsync, ChkPotPlayer));
            if (ChkFastStone.IsChecked == true) tasks.Add((InstallFastStoneAsync, ChkFastStone));
            if (ChkFoxit.IsChecked == true) tasks.Add((InstallFoxitAsync, ChkFoxit));
            if (ChkBandiview.IsChecked == true) tasks.Add((InstallBandiviewAsync, ChkBandiview));
            if (ChkRevoUninstaller.IsChecked == true) tasks.Add((InstallHibitUninstallerAsync, ChkRevoUninstaller));
            if (Chk3DPChip.IsChecked == true) tasks.Add((Run3DPChipAsync, Chk3DPChip));
            if (Chk3DPNet.IsChecked == true) tasks.Add((Install3DPNetAsync, Chk3DPNet));
            if (ChkMMTApps.IsChecked == true) tasks.Add((InstallMMTAppsAsync, ChkMMTApps));
            if (ChkDISMPP.IsChecked == true) tasks.Add((InstallDISMPPAsync, ChkDISMPP));
            if (ChkComfortClipboardPro.IsChecked == true) tasks.Add((InstallComfortClipboardProAsync, ChkComfortClipboardPro));
            if (ChkOfficeSoftmaker.IsChecked == true) tasks.Add((InstallOfficeSoftmakerAsync, ChkOfficeSoftmaker));
            if (ChkNotepadPP.IsChecked == true) tasks.Add((InstallNotepadPPAsync, ChkNotepadPP));
            if (ChkFonts.IsChecked == true) tasks.Add((InstallFontsAsync, ChkFonts));
            // Only add once to avoid duplicate install and MessageBox
            if (ChkPowerISO.IsChecked == true) tasks.Add((InstallPowerISOAsync, ChkPowerISO));
            if (ChkVPN1111.IsChecked == true) tasks.Add((InstallVPN1111Async, ChkVPN1111));
            if (ChkTeracopy.IsChecked == true) tasks.Add((InstallTeraCopyAsync, ChkTeracopy));
            if (ChkGoogleDrive.IsChecked == true) tasks.Add((InstallGoogleDriveAsync, ChkGoogleDrive));
            if (ChkNetLimiter.IsChecked == true) tasks.Add((InstallNetLimiterAsync, ChkNetLimiter));
            if (ChkFolderSize.IsChecked == true) tasks.Add((InstallFolderSizeAsync, ChkFolderSize));
            if (ChkDiskGenius.IsChecked == true) tasks.Add((InstallDiskGeniusAsync, ChkDiskGenius));
            if (ChkProcessLasso.IsChecked == true) tasks.Add((InstallProcessLassoAsync, ChkProcessLasso));
            if (ChkThrottlestop.IsChecked == true) tasks.Add((InstallThrottlestopAsync, ChkThrottlestop));
            if (ChkMSIAfterburner.IsChecked == true) tasks.Add((InstallMSIAfterburnerAsync, ChkMSIAfterburner));
            if (ChkLeagueOfLegends.IsChecked == true) tasks.Add((InstallLeagueOfLegendsVNAsync, ChkLeagueOfLegends));
            if (ChkPorofessor.IsChecked == true) tasks.Add((InstallPorofessorAsync, ChkPorofessor));
            if (ChkSamuraiMaiden.IsChecked == true) tasks.Add((InstallSamuraiMaidenAsync, ChkSamuraiMaiden));
            if (ChkAomeiPartitionAssistant.IsChecked == true) tasks.Add((InstallAomeiPartitionAssistantAsync, ChkAomeiPartitionAssistant));
            if (ChkUltraviewer.IsChecked == true) tasks.Add((InstallUltraviewerAsync, ChkUltraviewer));
            if (ChkTeamViewerQS.IsChecked == true) tasks.Add((InstallTeamViewerQuickSupportAsync, ChkTeamViewerQS));
            if (ChkTeamViewerFull.IsChecked == true) tasks.Add((InstallTeamViewerFullAsync, ChkTeamViewerFull));
            if (ChkAnyDesk.IsChecked == true) tasks.Add((InstallAnyDeskAsync, ChkAnyDesk));
            if (ChkVMWare162Lite.IsChecked == true) tasks.Add((InstallVMWare162LiteAsync, ChkVMWare162Lite));
            if (ChkAdvancedCodec.IsChecked == true) tasks.Add((InstallAdvancedCodecAsync, ChkAdvancedCodec));
            if (ChkWin11_26H1.IsChecked == true) tasks.Add((InstallWin11_26H1Async, ChkWin11_26H1));
            if (ChkWin10_20H2_2022April.IsChecked == true) tasks.Add((InstallWin10_20H2_2022AprilAsync, ChkWin10_20H2_2022April));
            if (ChkWin10LtscIot21H2.IsChecked == true) tasks.Add((InstallWin10LtscIot21H2Async, ChkWin10LtscIot21H2));

            CheckBox currentTaskCheckBox = null;
            try
            {
                foreach (var taskInfo in tasks)
                {
                    currentTaskCheckBox = taskInfo.CheckBox;

                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        UpdateStatus("Quá trình cài đặt đã bị dừng.", "Red");
                        break;
                    }

                    // Delay 500ms giữa các task để tránh tải nhiều file cùng lúc gây lỗi server
                    if (tasks.Count > 1)
                    {
                        await Task.Delay(500);
                    }

                    await taskInfo.Action();

                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        if (taskInfo.CheckBox != null)
                        {
                            taskInfo.CheckBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                        }
                        UpdateStatus("Quá trình cài đặt đã bị dừng.", "Red");
                        break;
                    }

                    if (taskInfo.CheckBox != null)
                    {
                        taskInfo.CheckBox.IsChecked = false;
                        taskInfo.CheckBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Cyan);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                if (currentTaskCheckBox != null)
                {
                    currentTaskCheckBox.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                }
                UpdateStatus("Quá trình cài đặt đã bị hủy.", "Red");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Đã xảy ra lỗi: {ex.Message}", "Red");
            }
            finally
            {
                BtnStop.IsEnabled = false;
                BtnPause.IsEnabled = false;
                UpdateInstallButtonState();
                UpdateStatus("Hoàn tất tất cả các tác vụ.", "Green");
                
                // Đặt lại trạng thái không còn cài đặt
                SetInstallingState(false);
            }
        }

        // Cache các link download khi hover
        private List<string> _cachedDownloadLinks = new List<string>();

        private void BtnDownloadPage_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Xây dựng danh sách link download khi hover
            _cachedDownloadLinks.Clear();

            if (ChkInstallIDM?.IsChecked == true)
                _cachedDownloadLinks.Add("https://tinyurl.com/idmhcmvn");

            if (ChkInstallWinRAR?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/WinRAR.7.13.exe");

            if (ChkInstallBID?.IsChecked == true)
                _cachedDownloadLinks.Add("https://bulkimagedownloader.com/download");

            if (ChkActivateWindows?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/activate/ACTIVATE.WINDOWS.cmd");

            if (ChkPauseWindowsUpdate?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/test/pause.update.win.11.ps1");

            if (ChkVcredist?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/vcredist.all.in.one.by.MMT.Windows.Tech.exe");

            if (ChkDirectX?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/DirectX.exe");

            if (ChkJava?.IsChecked == true)
                _cachedDownloadLinks.Add("https://javadl.oracle.com/webapps/download/AutoDL?BundleId=252627_99a6cb9582554a09bd4ac60f73f9b8e6");

            if (ChkOpenAL?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/OpenAL.exe");

            if (ChkChrome?.IsChecked == true)
                _cachedDownloadLinks.Add("https://dl.google.com/tag/s/appguid%3D%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D%26iid%3D%7BE9FD60DA-2FFA-E657-6449-67646C84E6C0%7D%26lang%3Dvi%26browser%3D5%26usagestats%3D1%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26ap%3Dx64-statsdef_1%26installdataindex%3Dempty/update2/installers/ChromeSetup.exe");

            if (ChkCocCoc?.IsChecked == true)
                _cachedDownloadLinks.Add("https://files.coccoc.com/browser/coccoc_standalone_vi.exe");

            if (ChkEdge?.IsChecked == true)
                _cachedDownloadLinks.Add("https://c2rsetup.officeapps.live.com/c2r/downloadEdge.aspx?platform=Default&source=EdgeStablePage&Channel=Stable&language=vi&brand=M100");

            if (ChkRevoUninstaller?.IsChecked == true)
                _cachedDownloadLinks.Add("https://www.hibitsoft.ir/HiBitUninstaller/RevoUninstaller-setup.exe");

            if (Chk3DPNet?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/3DP.Net.exe");

            if (Chk3DPChip?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/3DP.Chip.exe");

            if (ChkMMTApps?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/MMT.Apps.exe");

            if (ChkDISMPP?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/WinPE/DISM++.exe");

            if (ChkComfortClipboardPro?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Comfort.Clipboard.Pro.exe");

            if (ChkAomeiPartitionAssistant?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/AOMEI.Partition.Assistant.exe");

            if (ChkUltraviewer?.IsChecked == true)
                _cachedDownloadLinks.Add("https://dl2.ultraviewer.net/UltraViewer_setup_6.6_vi.exe");

            if (ChkPotPlayer?.IsChecked == true)
                _cachedDownloadLinks.Add("https://t1.daumcdn.net/potplayer/PotPlayer/Version/Latest/PotPlayerSetup64.exe");

            if (ChkFastStone?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/FastStone.Capture.exe");

            if (ChkFoxit?.IsChecked == true)
                _cachedDownloadLinks.Add("https://cdn01.foxitsoftware.com/product/reader/desktop/win/2025.2.0/FoxitPDFReader20252_L10N_Setup_Prom_x64.exe");

            if (ChkBandiview?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Bandiview.exe");

            if (ChkAdvancedCodec?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/ADVANCED_Codec_Pack.exe");

            if (ChkTeamViewerQS?.IsChecked == true)
                _cachedDownloadLinks.Add("https://dl.teamviewer.com/download/TeamViewerQS_x64.exe");

            if (ChkTeamViewerFull?.IsChecked == true)
                _cachedDownloadLinks.Add("https://tinyurl.com/teamviewerlatest");

            if (ChkAnyDesk?.IsChecked == true)
                _cachedDownloadLinks.Add("https://tinyurl.com/anydesk621");

            if (ChkVMWare162Lite?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/VMware_Workstation_16.2.2_Lite_Eng_._Rus.exe");

            if (ChkOfficeToolPlus?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/office.tool.plus.exe");

            if (ChkOfficeSoftmaker?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Office.Softmaker.exe");

            if (ChkNotepadPP?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.9.2/npp.8.9.2.Installer.exe");

            if (ChkPowerISO?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/PowerISO.exe");

            if (ChkVPN1111?.IsChecked == true)
                _cachedDownloadLinks.Add("https://1111-releases.cloudflareclient.com/win/latest");

            if (ChkTeracopy?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/TeraCopy.Pro.v3.17.0.0.exe");

            if (ChkGoogleDrive?.IsChecked == true)
                _cachedDownloadLinks.Add("https://dl.google.com/drive-file-stream/GoogleDriveSetup.exe");

            if (ChkNetLimiter?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/netlimiter-4.1.12.0.exe");

            if (ChkFolderSize?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/FolderSize-2.6-x64.msi");

            if (ChkDiskGenius?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Disk.Genius.exe");

            if (ChkProcessLasso?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Process.Lasso.exe");

            if (ChkThrottlestop?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Throttlestop.zip");

            if (ChkMSIAfterburner?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/MSI.Afterburner.exe");

            if (ChkLeagueOfLegends?.IsChecked == true)
                _cachedDownloadLinks.Add("https://lol.secure.dyn.riotcdn.net/channels/public/x/installer/current/live.vn2.exe");

            if (ChkPorofessor?.IsChecked == true)
                _cachedDownloadLinks.Add("https://download.overwolf.com/installer/prod/339334cdda5e1ea8a3c8a31ba816fb37/Porofessor%20Standalone%20-%20Installer.exe");

            if (ChkFonts?.IsChecked == true)
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Gouenji.Fansub.Fonts.exe");

            if (ChkWin11_26H1?.IsChecked == true)
                _cachedDownloadLinks.Add("https://archive.org/download/microsoft-win11-26h2-february-2026/en-us_windows_11_consumer_editions_version_26h1_x64_dvd_5208fe5b.iso");

            if (ChkWin10_20H2_2022April?.IsChecked == true)
                _cachedDownloadLinks.Add("https://glennsferryschools-my.sharepoint.com/:u:/g/personal/billgates_glennsferryschools_onmicrosoft_com/Ed8HqTyoPFxLktIGaRFqDOYBQP5hWqV8d69Qq9TJ-k9L0A?download=1");

            if (ChkSamuraiMaiden?.IsChecked == true)
            {
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/game/SAMURAI.MAIDEN_LinkNeverDie.Com.part1.exe");
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/game/SAMURAI.MAIDEN_LinkNeverDie.Com.part2.rar");
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/game/SAMURAI.MAIDEN_LinkNeverDie.Com.part3.rar");
                _cachedDownloadLinks.Add("https://github.com/ghostminhtoan/MMT/releases/download/game/SAMURAI.MAIDEN_LinkNeverDie.Com.part4.rar");
            }

            // Hiển thị tooltip với danh sách link
            string tooltipText;
            if (_cachedDownloadLinks.Count == 0)
            {
                tooltipText = "Vui lòng chọn (check) các checkbox ứng với phần mềm muốn tải để xem link download trực tiếp";
            }
            else
            {
                tooltipText = $"Click để mở {_cachedDownloadLinks.Count} link:\n" + string.Join("\n", _cachedDownloadLinks);
            }

            BtnDownloadPage.ToolTip = new System.Windows.Controls.ToolTip
            {
                Content = tooltipText,
                Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse
            };
        }

        private void BtnDownloadPage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Sử dụng danh sách link đã được cache khi hover
                if (_cachedDownloadLinks.Count == 0)
                {
                    UpdateStatus("Vui lòng chọn (check) các checkbox ứng với phần mềm muốn tải trước", "Orange");
                }
                else
                {
                    // Mở tất cả các liên kết đã chọn
                    foreach (var link in _cachedDownloadLinks)
                    {
                        Process.Start(link);
                    }
                    UpdateStatus($"Đã mở {_cachedDownloadLinks.Count} liên kết tải về tương ứng", "Green");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi mở trang download: {ex.Message}", "Red");
            }
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
                UpdateStatus("Đang dừng quá trình cài đặt...", "Yellow");
                BtnStop.IsEnabled = false;
                BtnPause.IsEnabled = false;
                BtnInstall.IsEnabled = true;

                // Resume event if cancelled while paused
                if (!_pauseEvent.IsSet)
                {
                    _pauseEvent.Set();
                    BtnPause.Content = "Pause";
                }

                // Đặt lại tiến độ khi bị hủy
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ConnectionTraceGrid.Children.Clear();
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });
            }
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            // Only allow pause during download
            if (_pauseEvent == null || !BtnPause.IsEnabled)
                return;

            if (_pauseEvent.IsSet)
            {
                // Đang chạy -> Tạm dừng
                _pauseEvent.Reset();
                if (_pauseCts != null && !_pauseCts.IsCancellationRequested)
                    _pauseCts.Cancel(); // Ngắt ngay lập tức mạng
                BtnPause.Content = "Resume";
                UpdateStatus("Đã tạm dừng quá trình tải xuống (Đang ngắt kết nối...)", "Yellow");
            }
            else
            {
                // Đang tạm dừng -> Chạy tiếp
                if (_pauseCts == null || _pauseCts.IsCancellationRequested)
                    _pauseCts = new CancellationTokenSource();
                _pauseEvent.Set();
                BtnPause.Content = "Pause";
                UpdateStatus("Đang tiếp tục quá trình tải xuống...", "Cyan");
            }
        }

        private void BtnRefreshColor_Click(object sender, RoutedEventArgs e)
        {
            var allCheckBoxes = new System.Windows.Controls.CheckBox[]
            {
                ChkInstallIDM, ChkInstallWinRAR, ChkInstallBID, ChkActivateWindows,
                ChkPauseWindowsUpdate, ChkVcredist, ChkDirectX, ChkJava, ChkOpenAL,
                Chk3DPChip, Chk3DPNet, ChkRevoUninstaller,
                ChkOfficeToolPlus, ChkOfficeSoftmaker, ChkActivateOffice, ChkFonts,
                ChkNotepadPP, ChkPotPlayer, ChkFastStone, ChkFoxit, ChkBandiview,
                ChkAdvancedCodec, ChkMMTApps, ChkDISMPP, ChkComfortClipboardPro,
                ChkFolderSize, ChkPowerISO, ChkVPN1111, ChkTeracopy, ChkGoogleDrive,
                ChkNetLimiter, ChkAomeiPartitionAssistant, ChkDiskGenius, ChkProcessLasso,
                ChkThrottlestop, ChkMSIAfterburner, ChkLeagueOfLegends, ChkPorofessor,
                ChkSamuraiMaiden, ChkChrome, ChkCocCoc, ChkEdge,
                ChkUltraviewer, ChkTeamViewerQS, ChkTeamViewerFull, ChkAnyDesk, ChkVMWare162Lite,
                ChkWin11_26H1,
                ChkWin10_20H2_2022April, ChkWin10LtscIot21H2,
                // ChkWin10ProWorkstations22H2 removed
            };

            foreach (var chk in allCheckBoxes)
            {
                if (chk != null)
                    chk.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
            }
        }

        // Hiển thị link download khi hover vào checkbox
        private void Checkbox_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                string link = null;
                switch (checkBox.Name)
                {
                    case "ChkInstallIDM":
                        link = "https://tinyurl.com/idmhcmvn";
                        break;
                    case "ChkInstallWinRAR":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/WinRAR.7.13.exe";
                        break;
                    case "ChkInstallBID":
                        link = "https://bulkimagedownloader.com/download";
                        break;
                    case "ChkActivateWindows":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/activate/ACTIVATE.WINDOWS.cmd";
                        break;
                    case "ChkPauseWindowsUpdate":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/test/pause.update.win.11.ps1";
                        break;
                    case "ChkVcredist":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/vcredist.all.in.one.by.MMT.Windows.Tech.exe";
                        break;
                    case "ChkDirectX":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/DirectX.exe";
                        break;
                    case "ChkJava":
                        link = "https://javadl.oracle.com/webapps/download/AutoDL?BundleId=252627_99a6cb9582554a09bd4ac60f73f9b8e6";
                        break;
                    case "ChkOpenAL":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/OpenAL.exe";
                        break;
                    case "Chk3DPChip":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/3DP.Chip.exe";
                        break;
                    case "Chk3DPNet":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/3DP.Net.exe";
                        break;
                    case "ChkRevoUninstaller":
                        link = "https://www.hibitsoft.ir/HiBitUninstaller/RevoUninstaller-setup.exe";
                        break;
                    case "ChkOfficeToolPlus":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/OfficeToolPlus.exe";
                        break;
                    case "ChkOfficeSoftmaker":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/SoftMaker.Office.exe";
                        break;
                    case "ChkNotepadPP":
                        link = "https://github.com/notepad-plus-plus/notepad-plus-plus/releases/download/v8.6.2/npp.8.6.2.Installer.x64.exe";
                        break;
                    case "ChkPotPlayer":
                        link = "https://t1.daumcdn.net/potplayer/PotPlayer/Version/Latest/PotPlayerSetup64.exe";
                        break;
                    case "ChkFastStone":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/FastStone.Capture.exe";
                        break;
                    case "ChkFoxit":
                        link = "https://cdn01.foxitsoftware.com/product/reader/desktop/win/2025.2.0/FoxitPDFReader20252_L10N_Setup_Prom_x64.exe";
                        break;
                    case "ChkBandiview":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Bandiview.exe";
                        break;
                    case "ChkMMTApps":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/MMT.Apps.exe";
                        break;
                    case "ChkDISMPP":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/WinPE/DISM++.exe";
                        break;
                    case "ChkComfortClipboardPro":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Comfort.Clipboard.Pro.exe";
                        break;
                    case "ChkPowerISO":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/PowerISO.exe";
                        break;
                    case "ChkTeracopy":
                        link = "https://www.codesector.com/files/teracopy.exe";
                        break;
                    case "ChkGoogleDrive":
                        link = "https://dl.google.com/drive-file-stream/GoogleDriveSetup.exe";
                        break;
                    case "ChkNetLimiter":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/netlimiter-4.1.12.0.exe";
                        break;
                    case "ChkAomeiPartitionAssistant":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/AOMEI.Partition.Assistant.exe";
                        break;
                    case "ChkDiskGenius":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/WinPE/Disk.Genius.exe";
                        break;
                    case "ChkProcessLasso":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/ProcessLasso.exe";
                        break;
                    case "ChkThrottlestop":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Throttlestop.exe";
                        break;
                    case "ChkMSIAfterburner":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/MSI.Afterburner.exe";
                        break;
                    case "ChkChrome":
                        link = "https://dl.google.com/tag/s/appguid%3D%7B8A69D345-D564-463C-AFF1-A69D9E530F96%7D%26iid%3D%7BE9FD60DA-2FFA-E657-6449-67646C84E6C0%7D%26lang%3Dvi%26browser%3D5%26usagestats%3D1%26appname%3DGoogle%2520Chrome%26needsadmin%3Dprefers%26ap%3Dx64-statsdef_1%26installdataindex%3Dempty/update2/installers/ChromeSetup.exe";
                        break;
                    case "ChkCocCoc":
                        link = "https://files.coccoc.com/browser/coccoc_standalone_vi.exe";
                        break;
                    case "ChkEdge":
                        link = "https://c2rsetup.officeapps.live.com/c2r/downloadEdge.aspx?platform=Default&source=EdgeStablePage&Channel=Stable&language=vi&brand=M100";
                        break;
                    case "ChkUltraviewer":
                        link = "https://dl2.ultraviewer.net/UltraViewer_setup_6.6_vi.exe";
                        break;
                    case "ChkTeamViewerQS":
                        link = "https://download.teamviewer.com/download/TeamViewerQS.exe";
                        break;
                    case "ChkTeamViewerFull":
                        link = "https://download.teamviewer.com/download/TeamViewer_Setup.exe";
                        break;
                    case "ChkAnyDesk":
                        link = "https://anydesk.com/en/downloads/thank-you?dv=win_exe";
                        break;
                    case "ChkVMWare162Lite":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/VMware.Workstation.Pro.16.2.Lite.exe";
                        break;
                    case "ChkWin11_26H1":
                        link = "https://archive.org/download/win-11-26h1";
                        break;
                    case "ChkActivateOffice":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/activate/activate-office.bat";
                        break;
                    case "ChkFonts":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/fonts/Fonts.Collection.exe";
                        break;
                    case "ChkAdvancedCodec":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Advanced.Codec.Pack.exe";
                        break;
                    case "ChkFolderSize":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/FolderSize.exe";
                        break;
                    case "ChkVPN1111":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/v1.1.1.1/1.1.1.1-setup.exe";
                        break;
                    case "ChkLeagueOfLegends":
                        link = "https://lol.qq.com/download/";
                        break;
                    case "ChkPorofessor":
                        link = "https://porofessor.gg/downloads";
                        break;
                    case "ChkSamuraiMaiden":
                        link = "https://github.com/ghostminhtoan/MMT/releases/download/game/SAMURAI.MAIDEN.LinkNeverDie.Com.part1.exe";
                        break;
                    case "ChkWin10_20H2_2022April":
                        link = "https://www.mediafire.com/file/your_file_id/Win10_20H2_2022_April.iso/file";
                        break;
                    case "ChkWin10LtscIot21H2":
                        link = "https://www.mediafire.com/file/your_file_id/Win10_LTSC_IoT_21H2_NoDefender_Office_MMTPC3.iso/file";
                        break;
                }

                if (!string.IsNullOrEmpty(link))
                {
                    checkBox.ToolTip = new ToolTip
                    {
                        Content = "Link: " + link,
                        Placement = System.Windows.Controls.Primitives.PlacementMode.Mouse,
                        HorizontalOffset = 10,
                        VerticalOffset = 10
                    };
                }
            }
        }
    }
}