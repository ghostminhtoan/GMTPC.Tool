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
        private void ActivateWindows()
        {
            UpdateStatus("Đang kích hoạt Windows...", "Cyan");
            string activateWindowsCmdPath = Path.Combine(GetGMTPCFolder(), "ACTIVATE.WINDOWS.cmd");
            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                     client.DownloadFile("https://github.com/ghostminhtoan/MMT/releases/download/activate/ACTIVATE.WINDOWS.cmd", activateWindowsCmdPath);
                }
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = activateWindowsCmdPath, UseShellExecute = true, Verb = "runas" };
                Process.Start(startInfo);
                UpdateStatus("Đã mở cửa sổ kích hoạt Windows", "Green");
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private void PauseWindowsUpdate()
        {
            UpdateStatus("Đang truy cập tính năng Pause Windows Update...", "Cyan");
            string stopUpdateCmdPath = Path.Combine(GetGMTPCFolder(), "Stop_Update.cmd");
            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                     client.DownloadFile("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Stop_Update.cmd", stopUpdateCmdPath);
                }
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = stopUpdateCmdPath, UseShellExecute = true, Verb = "runas" };
                Process.Start(startInfo);
                UpdateStatus("Đã mở công cụ Pause Windows Update", "Green");
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private async Task InstallAndActivateWinRARAsync()
        {
            await InstallWinRARAsync();
        }

        private async Task InstallAndActivateBIDAsync()
        {
            await InstallBIDAsync();
        }

        private async Task InstallVcredistAsync()
        {
            UpdateStatus("Đang tải Vcredist...", "Cyan");
            string vcredistPath = Path.Combine(GetGMTPCFolder(), "vcredist_x64.exe");
            try
            {
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/vcredist_x64.exe", vcredistPath, "Vcredist");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });
                UpdateStatus("Đang cài đặt Vcredist...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = vcredistPath, Arguments = "/install /passive /norestart", UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus("Cài đặt Vcredist hoàn tất.", "Green"); }
                if (File.Exists(vcredistPath)) File.Delete(vcredistPath);
            } catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private Task InstallDirectXAsync()
        {
            BtnDirectX_Click(null, null);
            return Task.CompletedTask;
        }

        private Task InstallJavaAsync()
        {
            BtnJava_Click(null, null);
            return Task.CompletedTask;
        }

        private Task InstallOpenALAsync()
        {
            BtnOpenAL_Click(null, null);
            return Task.CompletedTask;
        }

        private async Task InstallZaloAsync()
        {
            UpdateStatus("Đang tải Zalo...", "Cyan");
            string zaloPath = Path.Combine(GetGMTPCFolder(), "ZaloSetup.exe");
            try {
                await DownloadWithProgressAsync("https://res-zaloapp-aka.zdn.vn/mac/ZaloSetup.exe", zaloPath, "Zalo");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = zaloPath, UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus("Zalo hoàn tất.", "Green"); }
            } catch(Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private Task Run3DPChipAsync()
        {
            Btn3DPChip_Click(null, null);
            return Task.CompletedTask;
        }

        private Task Install3DPNetAsync()
        {
            Btn3DPNet_Click(null, null);
            return Task.CompletedTask;
        }

        private async Task WaitForIDM2TmpFileToDisappear()
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "IDM");
            for (int i = 0; i < 50; i++) // 5 seconds timeout
            {
                try
                {
                    if (Directory.Exists(tempFolder) && Directory.GetFiles(tempFolder, "IDM*.tmp").Length > 0)
                    {
                        await Task.Delay(100);
                    }
                    else break;
                }
                catch { break; }
            }
        }

        private void BtnVcredist_Click(object sender, RoutedEventArgs e)
        {
            ChkVcredist.IsChecked = true;
            _ = InstallVcredistAsync();
        }

        private void BtnInstallIDM_Click(object sender, RoutedEventArgs e)
        {
            ChkInstallIDM.IsChecked = true;
            _ = InstallIDMAsync();
        }

        private async Task RunAutomatedProcessAsync()
        {
            await InstallIDMAsync();
        }

        private void BtnActivateWindows_Click(object sender, RoutedEventArgs e)
        {
            ChkActivateWindows.IsChecked = true;
            _ = Task.Run(() => ActivateWindows());
        }

        private void BtnPauseWindowsUpdate_Click(object sender, RoutedEventArgs e)
        {
            ChkPauseWindowsUpdate.IsChecked = true;
            _ = Task.Run(() => PauseWindowsUpdate());
        }

        private void BtnInstallWinRAR_Click(object sender, RoutedEventArgs e)
        {
            ChkInstallWinRAR.IsChecked = true;
            _ = InstallWinRARAsync();
        }

        private void BtnInstallBID_Click(object sender, RoutedEventArgs e)
        {
            ChkInstallBID.IsChecked = true;
            _ = InstallBIDAsync();
        }

        private void BtnFixIDMExtension_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang mở các liên kết extension...", "Cyan");
            Process.Start("https://microsoftedge.microsoft.com/addons/detail/idm-integration-module/llbjbkhnmlidjebalopleeepgdfgcpec");
            Process.Start("https://chromewebstore.google.com/detail/idm-integration-module/ngpampappnmepgilojfohadhhmbhlaek");
        }

        private async void BtnCrackIDM_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải và chạy IDM crack...", "Cyan");
            string idmCrackPath = Path.Combine(GetGMTPCFolder(), "IDM_6.4x_Crack.exe");
            try
            {
                await DownloadSingleConnectionAsync("https://github.com/ghostminhtoan/MMT/releases/download/activate/IDM_6.4x_Crack.exe", idmCrackPath, "IDM Crack");
                Process.Start(idmCrackPath);
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private async void BtnRunBIDActivation_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải BID Crack...", "Cyan");
            string crackPath = Path.Combine(GetGMTPCFolder(), "BID_Crack.exe");
            try
            {
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Patch.exe", crackPath, "BID Crack");
                Process.Start(crackPath);
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private void ChkNotepadPP_Click(object sender, RoutedEventArgs e)
        {
            if (ChkNotepadPP.IsChecked == true) UpdateStatus("Đã chọn: Notepad++", "Green");
            else UpdateStatus("Đã hủy chọn: Notepad++", "Yellow");
            UpdateInstallButtonState();
        }
    }
}
