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
        // TabSystem — DISM++  (MessageBox + /s)
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
        // TabSystem — NetLimiter  (key dialog sau khi cài)
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
        // TabSystem — Comfort Clipboard Pro  (MessageBox + /passive)
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
        // TabSystem — MMT Apps  (MessageBox + /passive)
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
        // TabSystem — Defender Control  (MessageBox + VBS + /s -p1111)
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
        // TabSystem — Backup Restore Mklink MMT  (tải về Desktop + MessageBox)
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
        // TabPartition — Disk Genius  (MessageBox + /s)
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
        // TabPartition — AOMEI Partition Assistant  (MessageBox + /passive)
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
