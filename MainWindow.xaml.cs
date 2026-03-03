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
using System.Windows.Input; // Thêm directive này để sử dụng Key và Keyboard
using System.Net.Http;
using System.Windows.Controls;
using System.Windows.Data;
using System.Text;

namespace GMTPC.Tool
{
    public partial class MainWindow : Window
    {
        // Các phần mô phỏng phím đã được xóa
        private CancellationTokenSource _cancellationTokenSource;
        private CancellationTokenSource _pauseCts;
        private System.Threading.ManualResetEventSlim _pauseEvent = new System.Threading.ManualResetEventSlim(true);
        private List<DownloadRange> _remainingRanges = new List<DownloadRange>();
        private bool _isReSegmenting = false;
        public string TestProperty { get; set; } = "Hello";
        
        // Flag để theo dõi trạng thái đang cài đặt
        private bool _isInstalling = false;
        private string _installationStatus = "";
        private double originalWidth;
        private double originalHeight;

        public MainWindow()
        {
            System.Net.ServicePointManager.DefaultConnectionLimit = 100;
            System.Net.ServicePointManager.Expect100Continue = false;
            System.Net.ServicePointManager.UseNagleAlgorithm = false;
            InitializeComponent();
            UpdateStatus("Chọn một tùy chọn từ menu bên trên hoặc nhấn phím số tương ứng (1-6, 0):", "Cyan");
            this.Closing += MainWindow_Closing;
            UpdateInstallButtonState(); // Cập nhật trạng thái nút Install ban đầu

            // Initialize ComboBox with default selection
            if (CboDPIValue != null && CboDPIValue.Items.Count > 0)
            {
                // Select "100%" by default (new index after adding steps)
                CboDPIValue.SelectedIndex = 5; // 50,60,70,80,90,100 -> index 5
            }

            // Add Windows Defender exclusion when application starts
            AddDefenderExclusion();

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set focus to Window to enable keyboard shortcuts
            this.Focus();
            Keyboard.Focus(this);
            
            // Initialize dimensions for ConnectionTraceGrid
            SetupInitialOrientation();

            // Track display settings (resolution/orientation) changes
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;

            try
            {
                PopulateSystemInfo();
            }
            catch { }
        }

        // Hàm InitializeComponent sẽ được tạo tự động bởi WPF khi build, không cần định nghĩa lại


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Remove Windows Defender exclusion when application is closing
            RemoveDefenderExclusion();

            Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;

            // Fully shutdown the application when X button is clicked
            Application.Current.Shutdown();
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // Update orientation dependent layouts when screen rotates
            Dispatcher.Invoke(() =>
            {
                SetupInitialOrientation();
                ApplyDPIScale();
            });
        }

        private void UpdateStatus(string message, string color)
        {
            // Dùng InvokeAsync (non-blocking) để không block caller thread
            // Tránh deadlock khi background thread gọi UpdateStatus trong khi UI thread đang đợi
            Dispatcher.InvokeAsync(() =>
            {
                // Nếu đang trong quá trình cài đặt, lưu lại status và hiển thị ở dòng chính
                if (_isInstalling)
                {
                    _installationStatus = message;
                    ProgressTextBlock.Text = message;
                    ProgressTextBlock.Foreground = GetBrush(color);
                }
                else
                {
                    // Không đang cài đặt thì hiển thị bình thường
                    ProgressTextBlock.Text = message;
                    ProgressTextBlock.Foreground = GetBrush(color);
                }
            });
        }
        
        // Cập nhật status phụ (cho các thao tác như DPI change)
        private void UpdateSecondaryStatus(string message, string color = "Gray")
        {
            Dispatcher.InvokeAsync(() =>
            {
                SecondaryProgressTextBlock.Text = message;
                SecondaryProgressTextBlock.Foreground = GetBrush(color);
                
                // Nếu không đang cài đặt, dùng ProgressTextBlock làm status chính
                if (!_isInstalling)
                {
                    ProgressTextBlock.Text = message;
                    ProgressTextBlock.Foreground = GetBrush(color);
                }
            });
        }
        
        // Đặt trạng thái đang cài đặt
        private void SetInstallingState(bool isInstalling)
        {
            _isInstalling = isInstalling;
            if (!isInstalling)
            {
                // Khi kết thúc cài đặt, xóa secondary status
                Dispatcher.InvokeAsync(() =>
                {
                    SecondaryProgressTextBlock.Text = "";
                });
            }
        }
        
        private SolidColorBrush GetBrush(string colorName)
        {
            Color color = GetColor(colorName);
            return new SolidColorBrush(color);
        }

        private Color GetColor(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "red": return Colors.Yellow;
                default: return Colors.Yellow;
            }
        }

        private void ScrollToBottom()
        {
            // Không làm gì cả vì không còn điều khiển StatusScrollViewer
        }

        private async Task StartAutomatedProcessAsync()
        {
            await Task.Delay(500, _cancellationTokenSource?.Token ?? CancellationToken.None); // để UI render xong
            await RunAutomatedProcessAsync(); // Gọi trực tiếp phương thức async thay vì Task.Run
        }

        // ===================== helper cho GMTPC =====================
        private string GetGMTPCFolder()
        {
            string tempPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GMTPC", "GMTPC Tools");

            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }
            return tempPath;
        }

        private void AddDefenderExclusion()
        {
            try
            {
                string exclusionPath = GetGMTPCFolder();
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
                UpdateStatus($"Lỗi khi thêm exclusion cho Windows Defender: {ex.Message}", "Red");
            }
        }

        private void RemoveDefenderExclusion()
        {
            try
            {
                string exclusionPath = GetGMTPCFolder();
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

                if (Directory.Exists(exclusionPath))
                {
                    Directory.Delete(exclusionPath, true);
                }
            }
            catch { }
        }

        // ===================== Utility Methods =====================


        // Current DPI scale value (starts at 100%)
        private double currentDPIScale = 1.0;
        // Discrete DPI steps (percent)
        private readonly int[] DPI_STEPS = new int[] { 50, 60, 70, 80, 90, 100, 110, 120, 130, 140, 150, 160, 170, 180, 200 };

        private void ApplyDPIScale()
        {
            // Apply scale transform to the main grid
            ScaleTransform scaleTransform = new ScaleTransform(currentDPIScale, currentDPIScale);
            MainGrid.LayoutTransform = scaleTransform;

            bool isPortrait = SystemParameters.PrimaryScreenWidth < SystemParameters.PrimaryScreenHeight;
            
            // Adjust proportions for Portrait vs Landscape
            // Portrait: limit width to make the window a vertical rectangle
            // Landscape: limit the height and allow wide spanning for horizontal rectangle
            double designMaxWidth = isPortrait ? 580 : 1000;
            double designMaxHeight = isPortrait ? 950 : 750;

            // Dynamically adjust MaxHeight and MaxWidth based on DPI scale
            // Also ensure we don't exceed the usable work area (excludes taskbar)
            var workArea = SystemParameters.WorkArea;
            this.MaxHeight = Math.Min(designMaxHeight * currentDPIScale, workArea.Height);
            this.MaxWidth = Math.Min(designMaxWidth * currentDPIScale, workArea.Width);

            // Force window to re-measure and resize based on new content size
            MainGrid.InvalidateMeasure();
            this.InvalidateMeasure();
            
            // Use Dispatcher to ensure layout is updated before getting measurements
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Render, new Action(() =>
            {
                this.SizeToContent = SizeToContent.Manual;
                this.Width = double.NaN;
                this.Height = double.NaN;
                this.SizeToContent = SizeToContent.WidthAndHeight;
            }));

            // After layout/render, ensure window does not extend into the taskbar area
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                const double margin = 12.0; // safe spacing from edges/taskbar
                try
                {
                    // Ensure actual size does not exceed work area minus margin
                    double maxAllowedHeight = Math.Max(0, workArea.Height - margin);
                    double maxAllowedWidth = Math.Max(0, workArea.Width - margin);

                    if (this.ActualHeight > maxAllowedHeight)
                    {
                        this.Height = maxAllowedHeight;
                    }
                    if (this.ActualWidth > maxAllowedWidth)
                    {
                        this.Width = maxAllowedWidth;
                    }

                    // Reposition window if it would overlap the taskbar or go off-screen
                    if (this.Top < workArea.Top + margin) this.Top = workArea.Top + margin;
                    if (this.Left < workArea.Left + margin) this.Left = workArea.Left + margin;
                    if (this.Top + this.Height > workArea.Bottom - margin) this.Top = workArea.Bottom - margin - this.Height;
                    if (this.Left + this.Width > workArea.Right - margin) this.Left = workArea.Right - margin - this.Width;
                }
                catch
                {
                    // Swallow any layout exceptions to avoid crashing the UI
                }

                // Prevent further automatic resizing after we've adjusted position/size
                try { this.SizeToContent = SizeToContent.Manual; } catch { }
            }));

            // Update the ComboBox selection (only if needed to avoid recursion)
            int dpiPercent = (int)(currentDPIScale * 100);
            string dpiText = $"{dpiPercent}%";

            // Set ComboBox to the matching item
            ComboBoxItem selectedItem = CboDPIValue.SelectedItem as ComboBoxItem;
            string currentSelection = selectedItem?.Content.ToString() ?? "";

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

        private void ResetDPIButtonStates()
        {
            // This method is no longer used but kept for compatibility
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Xử lý Ctrl+Plus và Ctrl+Minus cho thay đổi DPI
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.Add || e.Key == Key.OemPlus) // Ctrl+Plus để tăng DPI
                {
                    BtnDPIPlus_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
                else if (e.Key == Key.Subtract || e.Key == Key.OemMinus) // Ctrl+Minus để giảm DPI
                {
                    BtnDPIMinus_Click(sender, new RoutedEventArgs());
                    e.Handled = true;
                    return;
                }
            }
        }



        private void Window_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            // Use mouse wheel alone to change DPI (no Ctrl required)
            if (e.Delta > 0) // Scroll up - tăng DPI
            {
                BtnDPIPlus_Click(sender, new RoutedEventArgs());
            }
            else if (e.Delta < 0) // Scroll down - giảm DPI
            {
                BtnDPIMinus_Click(sender, new RoutedEventArgs());
            }
            e.Handled = true; // prevent default scrolling
        }

        private async Task InstallMSIAfterbumerAsync()
        {
            try
            {
                UpdateStatus("Đang tải MSI Afterburner...", "Cyan");
                string msiAfterbumerPath = Path.Combine(GetGMTPCFolder(), "MSIAfterburner.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/MSI.Afterburner.exe", msiAfterbumerPath, "MSI Afterburner");

                UpdateStatus("Đang cài đặt MSI Afterburner...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = msiAfterbumerPath,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt MSI Afterburner hoàn tất!", "Green");
                }

                if (File.Exists(msiAfterbumerPath))
                {
                    File.Delete(msiAfterbumerPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt MSI Afterburner: {ex.Message}", "Red");
            }
        }

        private async Task InstallHibitUninstallerAsync()
        {
            try
            {
                UpdateStatus("Đang tải Revo Uninstaller...", "Cyan");
                string RevoPath = Path.Combine(GetGMTPCFolder(), "RevoUninstaller-setup.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Revo.Uninstaller.Pro.exe", RevoPath, "Revo Uninstaller Installer");

                // Reset progress UI after download
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ConnectionTraceGrid.Children.Clear();
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Revo Uninstaller với lệnh /S /I...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = RevoPath,
                    Arguments = "/S /I",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    if (process.ExitCode == 0)
                    {
                        UpdateStatus("Cài đặt Revo Uninstaller thành công!", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Cài đặt Revo Uninstaller thất bại. Mã lỗi: {process.ExitCode}", "Red");
                    }
                }

                if (File.Exists(RevoPath))
                {
                    File.Delete(RevoPath);
                    UpdateStatus("Đã xóa file Revo Uninstaller installer tạm thời", "Cyan");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải hoặc cài đặt Revo Uninstaller: {ex.Message}", "Red");
            }
        }


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
                             ChkZalo.IsChecked == true ||
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

        private void PopulateSystemInfo()
        {
            try
            {
                // Mainboard
                string mainboard = GetWmiSingleValue("Win32_BaseBoard", "Product") ?? GetWmiSingleValue("Win32_BaseBoard", "Manufacturer") ?? "Unknown";
                TbMainboard.Text = mainboard;

                // CPU
                string cpuName = GetWmiSingleValue("Win32_Processor", "Name") ?? "Unknown";
                string cpuClock = GetWmiSingleValue("Win32_Processor", "MaxClockSpeed");
                string cores = GetWmiSingleValue("Win32_Processor", "NumberOfCores");
                string threads = GetWmiSingleValue("Win32_Processor", "NumberOfLogicalProcessors");
                string cpuInfo = cpuName;
                if (!string.IsNullOrEmpty(cpuClock)) cpuInfo += $" - {cpuClock} MHz";
                cpuInfo += $" ({cores} cores / {threads} threads)";
                TbCPU.Text = cpuInfo;

                // RAM
                ulong totalRamBytes = 0;
                try
                {
                    var search = new ManagementObjectSearcher("select Capacity from Win32_PhysicalMemory");
                    foreach (ManagementObject mo in search.Get())
                    {
                        if (mo["Capacity"] != null)
                        {
                            ulong part = Convert.ToUInt64(mo["Capacity"]);
                            totalRamBytes += part;
                        }
                    }
                }
                catch { }
                TbRAM.Text = totalRamBytes > 0 ? FormatBytes(totalRamBytes) : "Unknown";

                // GPU
                string gpu = GetWmiSingleValue("Win32_VideoController", "Name") ?? "Unknown";
                TbGPU.Text = gpu;

                // Windows product and build
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion"))
                    {
                        if (key != null)
                        {
                            string productName = key.GetValue("ProductName") as string ?? "Windows";
                            string build = key.GetValue("CurrentBuild")?.ToString() ?? key.GetValue("CurrentBuildNumber")?.ToString() ?? "";
                            string ubr = key.GetValue("UBR")?.ToString();
                            string edition = key.GetValue("EditionID") as string ?? "";
                            string winText = productName;
                            if (!string.IsNullOrEmpty(edition)) winText += $" {edition}";
                            if (!string.IsNullOrEmpty(build)) winText += $" (Build {build}{(ubr != null ? "." + ubr : "")})";
                            TbWindows.Text = winText;
                        }
                    }
                }
                catch { TbWindows.Text = "Unknown"; }

                // DirectX version
                try
                {
                    TbDirectX.Text = GetDirectXVersion();
                }
                catch { TbDirectX.Text = "Unknown"; }
            }
            catch (Exception ex)
            {
                // don't crash UI
                try { TbMainboard.Text = "Error: " + ex.Message; } catch { }
            }
        }

        private string GetDirectXVersion()
        {
            try
            {
                // Most modern Windows (10/11) strictly use DirectX 12
                // We can also check the registry for a broad version string
                string versionStr = "";
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX"))
                {
                    versionStr = key?.GetValue("Version") as string ?? "";
                }

                // Map common internal version strings to friendly names
                if (versionStr.StartsWith("4.09.00.0904"))
                {
                    // This is the common string for DirectX 9.0c, but also often remains on Win10/11
                    // Detection by OS version is more reliable for modern DX
                    if (IsWindows10Or11()) return "DirectX 12";
                    return "DirectX 9.0c";
                }
                
                // Detailed mapping if needed
                switch (versionStr)
                {
                    case "4.09.00.0904": return "DirectX 9.0c";
                    case "4.09.00.0902": return "DirectX 9.0b";
                    case "4.09.00.0900": return "DirectX 9.0";
                    case "4.08.01.0881": return "DirectX 8.1";
                    case "4.08.00.0400": return "DirectX 8.0";
                    case "4.07.00.0700": return "DirectX 7.0";
                }

                if (IsWindows10Or11()) return "DirectX 12";
                if (IsWindows8Or81()) return "DirectX 11.1/11.2";
                if (IsWindows7()) return "DirectX 11";

                return !string.IsNullOrEmpty(versionStr) ? $"DirectX {versionStr}" : "Unknown";
            }
            catch { return "Unknown"; }
        }

        private bool IsWindows10Or11()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    string buildStr = key?.GetValue("CurrentBuild")?.ToString() ?? "";
                    if (int.TryParse(buildStr, out int build))
                    {
                        return build >= 10240; // Windows 10 build 10240 is the first release
                    }
                }
            }
            catch { }
            return Environment.OSVersion.Version.Major >= 10;
        }

        private bool IsWindows8Or81()
        {
            return Environment.OSVersion.Version.Major == 6 && (Environment.OSVersion.Version.Minor == 2 || Environment.OSVersion.Version.Minor == 3);
        }

        private bool IsWindows7()
        {
            return Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;
        }

        private string GetWmiSingleValue(string wmiClass, string property)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"select {property} from {wmiClass}");
                foreach (ManagementObject mo in searcher.Get())
                {
                    if (mo[property] != null)
                    {
                        return mo[property].ToString();
                    }
                }
            }
            catch { }
            return null;
        }

        private string FormatBytes(ulong bytes)
        {
            double gb = bytes / (1024.0 * 1024.0 * 1024.0);
            return $"{gb:F2} GB";
        }
    }
}

