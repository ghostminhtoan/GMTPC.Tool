// =======================================================================
// MainWindow.RestoredMethods.cs
// Chức năng: Hàm bổ trợ, phương thức dùng chung cho toàn ứng dụng
//            (GetBrush, GetColor, GetGMTPCFolder, Defender exclusion...)
// Cập nhật gần đây:
//   - 2026-03-07: Cập nhật các Install methods sử dụng constants từ
//                 SystemArguments.cs theo AI_WORKFLOW.md
//   - 2026-03-07: Fix Java installer exit code -1 handling, use JAVA_DOWNLOAD_URL
// =======================================================================
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // ===================== Color / Brush Helpers =====================
        private SolidColorBrush GetBrush(string colorName)
        {
            Color color = GetColor(colorName);
            return new SolidColorBrush(color);
        }

        private Color GetColor(string colorName)
        {
            switch (colorName.ToLower())
            {
                case "red":     return Colors.Red;
                case "green":   return Colors.LimeGreen;
                case "yellow":  return Colors.Yellow;
                case "cyan":    return Colors.Cyan;
                case "orange":  return Colors.Orange;
                case "gray":    return Colors.Gray;
                default:        return Colors.Yellow;
            }
        }

        // ===================== GMTPC Folder Helper =====================
        private string GetGMTPCFolder()
        {
            string tempPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GMTPC", "GMTPC Tools");

            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            return tempPath;
        }

        // ===================== Windows Defender Exclusion =====================
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
                if (process != null) process.WaitForExit();
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
                if (process != null) process.WaitForExit();

                if (Directory.Exists(exclusionPath))
                    Directory.Delete(exclusionPath, true);
            }
            catch { }
        }

        // ===================== Automated Process =====================
        private async Task StartAutomatedProcessAsync()
        {
            await Task.Delay(500, _cancellationTokenSource?.Token ?? CancellationToken.None);
            await RunAutomatedProcessAsync();
        }

        private void ScrollToBottom() { /* Không dùng */ }
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
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit();
                }
                UpdateStatus("Đã mở cửa sổ kích hoạt Windows", "Green");
                
                // Wait 2 seconds before showing message
                System.Threading.Thread.Sleep(2000);
                
                // Show message after activation completes
                UpdateStatus("Press \"0\" to continue", "Yellow");
                MessageBox.Show("Press \"0\" to continue", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private void PauseWindowsUpdate()
        {
            UpdateStatus("Đang truy cập tính năng Pause Windows Update...", "Cyan");
            string pauseUpdateScriptPath = Path.Combine(GetGMTPCFolder(), "pause.update.win.11.ps1");
            try
            {
                using (System.Net.WebClient client = new System.Net.WebClient())
                {
                     client.DownloadFile("https://github.com/ghostminhtoan/MMT/releases/download/test/pause.update.win.11.ps1", pauseUpdateScriptPath);
                }
                ProcessStartInfo startInfo = new ProcessStartInfo 
                { 
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -File \"{pauseUpdateScriptPath}\"",
                    UseShellExecute = true, 
                    Verb = "runas" 
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit();
                }
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

        private async Task InstallWinRARAsync()
        {
            string winrarPath = Path.Combine(GetGMTPCFolder(), "WinRAR.exe");
            try
            {
                // ===== Step 1: Download and Install WinRAR =====
                UpdateStatus("Đang tải WinRAR...", "Cyan");
                await DownloadWithRetryAsync(WINRAR_DOWNLOAD_URL, winrarPath, "WinRAR");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang chạy WinRAR installer ( " + WINRAR_INSTALL_ARGUMENTS + " )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = winrarPath, Arguments = WINRAR_INSTALL_ARGUMENTS, UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus("Cài đặt WinRAR hoàn tất.", "Green"); }
                if (File.Exists(winrarPath)) File.Delete(winrarPath);
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private async Task InstallBIDAsync()
        {
            string bidPath = Path.Combine(GetGMTPCFolder(), "bid_setup_x64.exe");
            string bidActivatePath = Path.Combine(GetGMTPCFolder(), "Bulk.image.downloader.patch.exe");
            string bidExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Bulk Image Downloader", "BID.exe");
            try
            {
                // ===== Step 1: Download and Install BID =====
                UpdateStatus("Đang tải Bulk Image Downloader...", "Cyan");
                string downloadUrl = await GetBIDDownloadLinkAsync();

                await DownloadWithProgressAsync(downloadUrl, bidPath, "Bulk Image Downloader");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang chạy BID installer...", "Yellow");
                ProcessStartInfo installStartInfo = new ProcessStartInfo { FileName = bidPath, Arguments = BID_INSTALL_ARGUMENTS, UseShellExecute = true };
                Process installProcess = Process.Start(installStartInfo);
                if (installProcess != null)
                {
                    await Task.Run(() => installProcess.WaitForExit());
                    UpdateStatus("Cài đặt BID hoàn tất.", "Green");
                }
                if (File.Exists(bidPath)) File.Delete(bidPath);

                // ===== Step 2: Show message BEFORE activation =====
                UpdateStatus("Click vào nút Install rồi tắt", "Yellow");
                MessageBox.Show("Click vào nút Install rồi tắt", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                // ===== Step 3: Download and Run Activation =====
                await Task.Delay(1000);
                UpdateStatus("Đang tải BID patch...", "Cyan");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/activate/Bulk.image.downloader.patch.exe", bidActivatePath, "BID Patch");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang chạy BID patch...", "Yellow");
                ProcessStartInfo activateStartInfo = new ProcessStartInfo { FileName = bidActivatePath, UseShellExecute = true };
                Process activateProcess = Process.Start(activateStartInfo);
                if (activateProcess != null)
                {
                    await Task.Run(() => activateProcess.WaitForExit());
                }
                if (File.Exists(bidActivatePath)) File.Delete(bidActivatePath);

                // ===== Step 4: Wait for BID to close =====
                UpdateStatus("Đang chờ BID tắt...", "Cyan");
                while (true)
                {
                    Process[] bidProcesses = Process.GetProcessesByName("BID");
                    if (bidProcesses.Length == 0) break;
                    await Task.Delay(500);
                }
                await Task.Delay(1000);

                // ===== Step 5: Open BID.exe =====
                if (File.Exists(bidExePath))
                {
                    UpdateStatus("Đang mở BID...", "Green");
                    Process.Start(bidExePath);
                }
                else
                {
                    UpdateStatus("Không tìm thấy BID.exe", "Yellow");
                }
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        /// <summary>
        /// Lấy link tải BID mới nhất từ bulkimagedownloader.com
        /// Tìm link có dạng: bid_*_*_setup_x64.exe
        /// </summary>
        private async Task<string> GetBIDDownloadLinkAsync()
        {
            try
            {
                UpdateStatus("Đang lấy link tải BID mới nhất...", "Cyan");
                
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                    
                    // Tải trang download
                    string html = await client.GetStringAsync("https://bulkimagedownloader.com/download");
                    
                    // Tìm link có dạng bid_*_*_setup_x64.exe
                    string pattern = @"/files/bid_\d+_\d+_setup_x64\.exe";
                    System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(html, pattern);
                    
                    if (match.Success)
                    {
                        string downloadPath = match.Value;
                        string fullUrl = "https://bulkimagedownloader.com" + downloadPath;
                        UpdateStatus($"Đã tìm thấy link BID mới: {fullUrl}", "Green");
                        return fullUrl;
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Không thể lấy link tự động, dùng link dự phòng: {ex.Message}", "Yellow");
            }
            
            // Fallback link cũ nếu không tìm thấy
            return "https://bulkimagedownloader.com/files/bid_6_62_setup_x64.exe";
        }

        private async Task InstallIDMAsync()
        {
            string idmPath = Path.Combine(GetGMTPCFolder(), "idman625build3.exe");
            string activatePath = Path.Combine(GetGMTPCFolder(), "IDM_6.4x_rabbit.exe");
            string idmExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Internet Download Manager", "IDMan.exe");
            string idmBackupPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Internet Download Manager", "IDMan.exe.bak");
            string tempCheckPath = Path.Combine(Path.GetTempPath(), "IDM_Setup_Temp", "IDM0.tmp");
            try
            {
                // ===== Step 1: Kill IDM =====
                UpdateStatus("Đang đóng IDM (nếu đang chạy)...", "Cyan");
                KillProcessByName("idman");
                await Task.Delay(500);

                // ===== Step 2: Delete backup file if exists =====
                if (File.Exists(idmBackupPath))
                {
                    UpdateStatus("Đang dọn dẹp tệp sao lưu cũ...", "Cyan");
                    File.Delete(idmBackupPath);
                }

                // ===== Step 3: Download and install IDM =====
                UpdateStatus("Đang tải Internet Download Manager...", "Cyan");
                await DownloadSingleConnectionAsync(IDM_DOWNLOAD_URL, idmPath, "Internet Download Manager");

                // Reset progress bar
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Run installer with arguments
                UpdateStatus("Đang chạy IDM installer ( " + IDM_INSTALL_ARGUMENTS + " )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = idmPath,
                    Arguments = IDM_INSTALL_ARGUMENTS,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                }

                // ===== Step 4: Download and run activate tool =====
                UpdateStatus("Đang tải công cụ kích hoạt...", "Cyan");
                await DownloadWithProgressAsync(IDM_ACTIVATE_URL, activatePath, "IDM Activate");

                // Reset progress bar
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // ===== Step 4.1: Wait for IDM0.tmp to disappear before running crack =====
                UpdateStatus("Đang chờ IDM hoàn tất cài đặt (IDM0.tmp biến mất)...", "Yellow");
                while (File.Exists(tempCheckPath))
                {
                    await Task.Delay(500);
                }
                await Task.Delay(1000); // Thêm delay nhỏ để đảm bảo ổn định

                // Run activate tool
                UpdateStatus("Click IDM to activate, có thể bỏ qua update.", "Yellow");
                ProcessStartInfo activateStartInfo = new ProcessStartInfo
                {
                    FileName = activatePath,
                    UseShellExecute = true
                };
                Process activateProcess = Process.Start(activateStartInfo);
                if (activateProcess != null)
                {
                    await Task.Run(() => activateProcess.WaitForExit());

                    if (File.Exists(activatePath))
                    {
                        File.Delete(activatePath);
                        UpdateStatus("Đã xóa công cụ kích hoạt", "Cyan");
                    }
                }

                // ===== Step 5: Open browser tabs for IDM integration =====
                UpdateStatus("Đang mở trang tích hợp IDM cho trình duyệt...", "Cyan");
                Process.Start("https://microsoftedge.microsoft.com/addons/detail/idm-integration-module/llbjbkhnmlidjebalopleeepgdfgcpec");
                Process.Start("https://chromewebstore.google.com/detail/idm-integration-module/ngpampappnmepgilojfohadhhmbhlaek");
                await Task.Delay(1000);

                // ===== Step 6: Check temp file and delete installer =====
                if (!File.Exists(tempCheckPath))
                {
                    if (File.Exists(idmPath))
                    {
                        File.Delete(idmPath);
                        UpdateStatus("Đã xóa file cài đặt IDM", "Cyan");
                    }
                }

                // ===== Step 7: Post-Install Loop (5 times: Open IDM -> Kill IDM) =====
                for (int i = 0; i < 5; i++)
                {
                    if (File.Exists(idmExePath))
                    {
                        UpdateStatus($"Lần {i + 1}/5: Đang mở IDM...", "Cyan");
                        Process.Start(idmExePath);
                    }

                    await Task.Delay(1500);

                    UpdateStatus($"Lần {i + 1}/5: Đang đóng IDM...", "Cyan");
                    KillProcessByName("idman");

                    if (i < 4)
                    {
                        await Task.Delay(1500);
                    }
                }

                UpdateStatus("Đã cài xong IDM!", "Green");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài IDM: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Kills all processes with the specified name
        /// </summary>
        private void KillProcessByName(string processName)
        {
            try
            {
                Process[] processes = Process.GetProcessesByName(processName);
                foreach (Process process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                            process.WaitForExit();
                        }
                    }
                    catch
                    {
                        // Ignore if process cannot be killed
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch
            {
                // Ignore if cannot get processes by name
            }
        }

        private async Task InstallVcredistAsync()
        {
            UpdateStatus("Đang tải Vcredist...", "Cyan");
            string vcredistPath = Path.Combine(GetGMTPCFolder(), "vcredist.all.in.one.by.MMT.Windows.Tech.exe");
            try
            {
                await DownloadWithProgressAsync(VCREDIST_DOWNLOAD_URL, vcredistPath, "Vcredist");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });
                UpdateStatus("Đang cài đặt Vcredist ( " + VCREDIST_INSTALL_ARGUMENTS + " )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = vcredistPath, Arguments = VCREDIST_INSTALL_ARGUMENTS, UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus("Cài đặt Vcredist hoàn tất.", "Green"); }
                if (File.Exists(vcredistPath)) File.Delete(vcredistPath);
            } catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private async Task InstallDirectXAsync()
        {
            UpdateStatus("Đang tải DirectX...", "Cyan");
            string directxPath = Path.Combine(GetGMTPCFolder(), "DirectX.exe");
            try
            {
                await DownloadWithProgressAsync(DIRECTX_DOWNLOAD_URL, directxPath, "DirectX");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });
                UpdateStatus("Đang cài đặt DirectX ( " + DIRECTX_INSTALL_ARGUMENTS + " )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = directxPath, Arguments = DIRECTX_INSTALL_ARGUMENTS, UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus("Cài đặt DirectX hoàn tất.", "Green"); }
                if (File.Exists(directxPath)) File.Delete(directxPath);
            } catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
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
                await DownloadWithProgressAsync(ZALO_DOWNLOAD_URL, zaloPath, "Zalo");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = zaloPath, Arguments = ZALO_INSTALL_ARGUMENTS, UseShellExecute = true };
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
            string bidActivatePath = Path.Combine(GetGMTPCFolder(), "Bulk.image.downloader.patch.exe");
            string bidExePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Bulk Image Downloader", "BID.exe");
            try
            {
                // ===== Step 1: Show message =====
                UpdateStatus("Click vào nút Install rồi tắt", "Yellow");
                MessageBox.Show("Click vào nút Install rồi tắt", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);

                // ===== Step 2: Download and Run Activation =====
                await Task.Delay(1000);
                UpdateStatus("Đang tải BID patch...", "Cyan");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/activate/Bulk.image.downloader.patch.exe", bidActivatePath, "BID Patch");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang chạy BID patch...", "Yellow");
                ProcessStartInfo activateStartInfo = new ProcessStartInfo { FileName = bidActivatePath, UseShellExecute = true };
                Process activateProcess = Process.Start(activateStartInfo);
                if (activateProcess != null)
                {
                    await Task.Run(() => activateProcess.WaitForExit());
                }
                if (File.Exists(bidActivatePath)) File.Delete(bidActivatePath);

                // ===== Step 3: Wait for BID to close =====
                UpdateStatus("Đang chờ BID tắt...", "Cyan");
                while (true)
                {
                    Process[] bidProcesses = Process.GetProcessesByName("BID");
                    if (bidProcesses.Length == 0) break;
                    await Task.Delay(500);
                }
                await Task.Delay(1000);

                // ===== Step 4: Open BID.exe =====
                if (File.Exists(bidExePath))
                {
                    UpdateStatus("Đang mở BID...", "Green");
                    Process.Start(bidExePath);
                }
                else
                {
                    UpdateStatus("Không tìm thấy BID.exe", "Yellow");
                }
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private void ChkNotepadPP_Click(object sender, RoutedEventArgs e)
        {
            if (ChkNotepadPP.IsChecked == true) UpdateStatus("Đã chọn: Notepad++", "Green");
            else UpdateStatus("Đã hủy chọn: Notepad++", "Yellow");
            UpdateInstallButtonState();
        }

        // ===================== TabPopular — Button Click Handlers =====================
        private async void BtnDirectX_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải DirectX...", "Cyan");
            string directXPath = Path.Combine(GetGMTPCFolder(), "directx_installer.exe");
            try
            {
                await DownloadWithProgressAsync("https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E99C9C/dxwebsetup.exe", directXPath, "DirectX Installer");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });
                UpdateStatus("Đang chạy DirectX installer với lệnh /q...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = directXPath, Arguments = "/q", UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus(process.ExitCode == 0 ? "Cài đặt DirectX thành công!" : $"Mã lỗi: {process.ExitCode}", process.ExitCode == 0 ? "Green" : "Red"); }
                if (File.Exists(directXPath)) File.Delete(directXPath);
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private async void BtnJava_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải Java...", "Cyan");
            string javaInstallerPath = Path.Combine(GetGMTPCFolder(), "java_installer.exe");
            try
            {
                await DownloadWithProgressAsync(JAVA_DOWNLOAD_URL, javaInstallerPath, "Java Installer");
                UpdateStatus("Đang chạy Java installer...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = javaInstallerPath, Arguments = JAVA_INSTALL_ARGUMENTS, UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    // Java web installer thường trả -1 hoặc 16389 khi thành công
                    bool isSuccess = process.ExitCode == 0 || process.ExitCode == -1 || process.ExitCode == 16389;
                    UpdateStatus(isSuccess ? "Cài đặt Java thành công!" : $"Mã lỗi: {process.ExitCode}", isSuccess ? "Green" : "Red");
                }
                if (File.Exists(javaInstallerPath)) File.Delete(javaInstallerPath);
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private async void BtnOpenAL_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải OpenAL...", "Cyan");
            string openALInstallerPath = Path.Combine(GetGMTPCFolder(), "OpenAL.exe");
            try
            {
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/OpenAL.exe", openALInstallerPath, "OpenAL Installer");
                UpdateStatus("Đang chạy OpenAL installer với lệnh /s...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = openALInstallerPath, Arguments = "/s", UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus(process.ExitCode == 0 ? "Cài đặt OpenAL thành công!" : $"Mã lỗi: {process.ExitCode}", process.ExitCode == 0 ? "Green" : "Red"); }
                if (File.Exists(openALInstallerPath)) File.Delete(openALInstallerPath);
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }

        private async void Btn3DPChip_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang chạy 3DP Chip - all driver trừ internet...", "Cyan");
            string driverChipPath = Path.Combine(@"R:\HDD R\ZC SYMLINK\USERS\Downloads\Programs", "3DP_Chip_v2510.exe");
            if (File.Exists(driverChipPath))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo { FileName = driverChipPath, UseShellExecute = true };
                    Process process = Process.Start(startInfo);
                    if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus(process.ExitCode == 0 ? "3DP Chip hoàn tất!" : $"Mã lỗi: {process.ExitCode}", process.ExitCode == 0 ? "Green" : "Red"); }
                }
                catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
            }
            else { UpdateStatus("Không tìm thấy file 3DP_Chip_v2510.exe", "Red"); }
        }

        private async void Btn3DPNet_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải 3DP Net - driver internet...", "Cyan");
            string driverNetPath = Path.Combine(GetGMTPCFolder(), "3DP_Net_v2101.exe");
            try
            {
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/3DP.Net.exe", driverNetPath, "3DP Net Driver Installer");
                UpdateStatus("Đang chạy 3DP Net với lệnh /y...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = driverNetPath, Arguments = "/y", UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus(process.ExitCode == 0 ? "3DP Net hoàn tất!" : $"Mã lỗi: {process.ExitCode}", process.ExitCode == 0 ? "Green" : "Red"); }
                if (File.Exists(driverNetPath)) File.Delete(driverNetPath);
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }
    }
}
