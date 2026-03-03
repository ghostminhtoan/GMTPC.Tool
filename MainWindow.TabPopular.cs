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

        private void ChkInstallIDM_Click(object sender, RoutedEventArgs e)
        {
            if (ChkInstallIDM.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Internet Download Manager", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Internet Download Manager", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkInstallWinRAR_Click(object sender, RoutedEventArgs e)
        {
            if (ChkInstallWinRAR.IsChecked == true)
            {
                UpdateStatus("Đã chọn: WinRAR", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: WinRAR", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkInstallBID_Click(object sender, RoutedEventArgs e)
        {
            if (ChkInstallBID.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Bulk Image Downloader", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Bulk Image Downloader", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkActivateWindows_Click(object sender, RoutedEventArgs e)
        {
            if (ChkActivateWindows.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Tự động kích hoạt Windows", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Tự động kích hoạt Windows", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkPauseWindowsUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (ChkPauseWindowsUpdate.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Pause Windows Update", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Pause Windows Update", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkVcredist_Click(object sender, RoutedEventArgs e)
        {
            if (ChkVcredist.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Vcredist 2005-2022", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Vcredist 2005-2022", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkDirectX_Click(object sender, RoutedEventArgs e)
        {
            if (ChkDirectX.IsChecked == true)
            {
                UpdateStatus("Đã chọn: DirectX", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: DirectX", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkJava_Click(object sender, RoutedEventArgs e)
        {
            if (ChkJava.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Java", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Java", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkOpenAL_Click(object sender, RoutedEventArgs e)
        {
            if (ChkOpenAL.IsChecked == true)
            {
                UpdateStatus("Đã chọn: OpenAL", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: OpenAL", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void Chk3DPChip_Click(object sender, RoutedEventArgs e)
        {
            if (Chk3DPChip.IsChecked == true)
            {
                UpdateStatus("Đã chọn: 3DP Chip", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: 3DP Chip", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void Chk3DPNet_Click(object sender, RoutedEventArgs e)
        {
            if (Chk3DPNet.IsChecked == true)
            {
                UpdateStatus("Đã chọn: 3DP Net", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: 3DP Net", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkRevoUninstaller_Click(object sender, RoutedEventArgs e)
        {
            if (ChkRevoUninstaller.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Revo Uninstaller", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Revo Uninstaller", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkZalo_Click(object sender, RoutedEventArgs e)
        {
            if (ChkZalo.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Zalo", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Zalo", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private async Task InstallIDMAsync()
        {
            UpdateStatus("Đang tải Internet Download Manager...", "Cyan");
            string idmPath = Path.Combine(GetGMTPCFolder(), "idman625build3.exe");
            try
            {
                await DownloadSingleConnectionAsync("https://tinyurl.com/idmhcmvn", idmPath, "Internet Download Manager");
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });
                UpdateStatus("Đang chạy IDM installer ( /s )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = idmPath,
                    Arguments = "/s",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt IDM hoàn tất.", "Green");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài IDM: {ex.Message}", "Red");
            }
        }

        
        private async Task InstallWinRARAsync()
        {
            UpdateStatus("Đang tải WinRAR...", "Cyan");
            string winrarPath = Path.Combine(GetGMTPCFolder(), "winrar-x64-621.exe");
            try
            {
                await DownloadWithProgressAsync("https://www.rarlab.com/rar/winrar-x64-621.exe", winrarPath, "WinRAR");
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });
                UpdateStatus("Đang chạy WinRAR installer ( /S )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = winrarPath,
                    Arguments = "/S",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt WinRAR hoàn tất.", "Green");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài WinRAR: {ex.Message}", "Red");
            }
        }


        private async Task InstallBIDAsync()
        {
            UpdateStatus("Đang tải Bulk Image Downloader...", "Cyan");
            string bidPath = Path.Combine(GetGMTPCFolder(), "bid_6_36_setup.exe");
            try
            {
                await DownloadWithProgressAsync("https://bulkimagedownloader.com/files/bid_6_36_setup.exe", bidPath, "Bulk Image Downloader");
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });
                UpdateStatus("Đang chạy BID installer ( /S )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = bidPath,
                    Arguments = "/S",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt BID hoàn tất.", "Green");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài BID: {ex.Message}", "Red");
            }
        }


        // ===================== Chức năng cài đặt DirectX =====================
        private async void BtnDirectX_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải DirectX...", "Cyan");
            string directXPath = Path.Combine(GetGMTPCFolder(), "directx_installer.exe");
            try
            {
                // Sử dụng DownloadWithProgressAsync để tải DirectX với URL từ yêu cầu
                await DownloadWithProgressAsync("https://download.microsoft.com/download/1/7/1/1718CCC4-6315-4D8E-9543-8E28A4E99C9C/dxwebsetup.exe", directXPath, "DirectX Installer");

                // Reset progress bar sau khi tải xong
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy DirectX installer với lệnh /q...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = directXPath,
                    Arguments = "/q", // Lệnh /q cho chế độ quiet/silent
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit()); // Chờ không đồng bộ
                    if (process.ExitCode == 0)
                    {
                        UpdateStatus("Cài đặt DirectX thành công!", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Cài đặt DirectX thất bại. Mã lỗi: {process.ExitCode}", "Red");
                    }
                }
                // Xóa file installer sau khi chạy xong (tùy chọn)
                if (File.Exists(directXPath))
                {
                    File.Delete(directXPath);
                    UpdateStatus("Đã xóa file DirectX installer tạm thời", "Cyan");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải hoặc cài đặt DirectX: {ex.Message}", "Red");
            }
        }


        // ===================== Chức năng cài đặt Java =====================
        private async void BtnJava_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải Java...", "Cyan");
            string javaInstallerPath = Path.Combine(GetGMTPCFolder(), "java_installer.exe");
            try
            {
                // Sử dụng DownloadWithProgressAsync để tải Java với URL từ yêu cầu
                await DownloadWithProgressAsync("https://javadl.oracle.com/webapps/download/AutoDL?BundleId=252627_99a6cb9582554a09bd4ac60f73f9b8e6", javaInstallerPath, "Java Installer");
                UpdateStatus("Đang chạy Java installer với lệnh /s...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = javaInstallerPath,
                    Arguments = "/s", // Lệnh /s cho chế độ silent
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit()); // Chờ không đồng bộ
                    if (process.ExitCode == 0)
                    {
                        UpdateStatus("Cài đặt Java thành công!", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Cài đặt Java thất bại. Mã lỗi: {process.ExitCode}", "Red");
                    }
                }
                // Xóa file installer sau khi chạy xong (tùy chọn)
                if (File.Exists(javaInstallerPath))
                {
                    File.Delete(javaInstallerPath);
                    UpdateStatus("Đã xóa file Java installer tạm thời", "Cyan");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải hoặc cài đặt Java: {ex.Message}", "Red");
            }
        }


        // ===================== Chức năng cài đặt OpenAL =====================
        private async void BtnOpenAL_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải OpenAL...", "Cyan");
            string openALInstallerPath = Path.Combine(GetGMTPCFolder(), "OpenAL.exe");
            try
            {
                // Sử dụng DownloadWithProgressAsync để tải Openal với URL từ yêu cầu
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/OpenAL.exe", openALInstallerPath, "OpenAL Installer");
                UpdateStatus("Đang chạy OpenAL installer với lệnh /s...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = openALInstallerPath,
                    Arguments = "/s", // Lệnh /s cho chế độ silent
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit()); // Chờ không đồng bộ
                    if (process.ExitCode == 0)
                    {
                        UpdateStatus("Cài đặt OpenAL thành công!", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Cài đặt OpenAL thất bại. Mã lỗi: {process.ExitCode}", "Red");
                    }
                }
                // Xóa file installer sau khi chạy xong (tùy chọn)
                if (File.Exists(openALInstallerPath))
                {
                    File.Delete(openALInstallerPath);
                    UpdateStatus("Đã xóa file OpenAL installer tạm thời", "Cyan");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải hoặc cài đặt OpenAL: {ex.Message}", "Red");
            }
        }


        // ===================== Chức năng cài đặt 3DP Chip =====================
        private async void Btn3DPChip_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang chạy 3DP Chip - all driver trừ internet...", "Cyan");
            string driverChipPath = Path.Combine(@"R:\HDD R\ZC SYMLINK\USERS\Downloads\Programs", "3DP_Chip_v2510.exe");

            if (File.Exists(driverChipPath))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = driverChipPath,
                        UseShellExecute = true
                    };
                    Process process = Process.Start(startInfo);
                    if (process != null)
                    {
                        await Task.Run(() => process.WaitForExit()); // Chờ không đồng bộ
                        if (process.ExitCode == 0)
                        {
                            UpdateStatus("3DP Chip - all driver trừ internet hoàn tất!", "Green");
                        }
                        else
                        {
                            UpdateStatus($"3DP Chip - all driver trừ internet thất bại. Mã lỗi: {process.ExitCode}", "Red");
                        }
                    }
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Lỗi khi chạy 3DP Chip: {ex.Message}", "Red");
                }
            }
            else
            {
                UpdateStatus("Không tìm thấy file 3DP_Chip_v2510.exe tại vị trí R:\\HDD R\\ZC SYMLINK\\USERS\\Downloads\\Programs\\3DP_Chip_v2510.exe", "Red");
            }
        }


        // ===================== Chức năng cài đặt 3DP Net =====================
        private async void Btn3DPNet_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải 3DP Net - driver internet...", "Cyan");
            string driverNetPath = Path.Combine(GetGMTPCFolder(), "3DP_Net_v2101.exe");

            try
            {
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/3DP.Net.exe", driverNetPath, "3DP Net Driver Installer");
                UpdateStatus("Đang chạy 3DP Net với lệnh /y...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = driverNetPath,
                    Arguments = "/y",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit()); // Chờ không đồng bộ
                    if (process.ExitCode == 0)
                    {
                        UpdateStatus("3DP Net - driver internet hoàn tất!", "Green");
                    }
                    else
                    {
                        UpdateStatus($"3DP Net - driver internet thất bại. Mã lỗi: {process.ExitCode}", "Red");
                    }
                }
                // Xóa file installer sau khi chạy xong
                if (File.Exists(driverNetPath))
                {
                    File.Delete(driverNetPath);
                    UpdateStatus("Đã xóa file 3DP Net installer tạm thời", "Cyan");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải hoặc chạy 3DP Net: {ex.Message}", "Red");
            }
        }

    }
}
