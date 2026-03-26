// =======================================================================
// MainWindow.TabPopular.cs
// Chức năng: Xử lý logic cho các checkbox/button trong tab Popular
// Cập nhật gần đây:
//   - 2026-03-25: Added Theme Toggle Button - IsDarkThemeEnabled(), 
//                 ToggleThemeAsync(), TglTheme_Click(), UpdateThemeToggleButtonState()
//                 Tự động nhận biết Dark/Light theme và restart Explorer sau khi đổi
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

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        /// <summary>
        /// Kiểm tra Windows đang dùng Dark Theme hay Light Theme
        /// Trả về true nếu đang dùng Dark Theme, false nếu đang dùng Light Theme
        /// </summary>
        private bool IsDarkThemeEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object value = key.GetValue("AppsUseLightTheme");
                        if (value != null)
                        {
                            int appsUseLightTheme = Convert.ToInt32(value);
                            // AppsUseLightTheme = 0: Dark Theme
                            // AppsUseLightTheme = 1: Light Theme
                            return appsUseLightTheme == 0;
                        }
                    }
                }
                return false; // Default là Light Theme
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Bật/tắt Dark Theme và restart Explorer
        /// </summary>
        private async Task ToggleThemeAsync()
        {
            try
            {
                bool isDarkTheme = IsDarkThemeEnabled();
                int newValue = isDarkTheme ? 1 : 0; // Nếu đang Dark thì đổi thành Light (1), ngược lại

                // Ghi registry
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize", true))
                {
                    if (key != null)
                    {
                        key.SetValue("AppsUseLightTheme", newValue, RegistryValueKind.DWord);
                        key.SetValue("SystemUsesLightTheme", newValue, RegistryValueKind.DWord);
                    }
                }

                UpdateStatus(isDarkTheme ? "Đang chuyển sang Light Theme..." : "Đang chuyển sang Dark Theme...", "Cyan");
                await Task.Delay(500);

                // Restart Explorer
                UpdateStatus("Đang restart Explorer...", "Yellow");
                
                var explorerProcesses = Process.GetProcessesByName("explorer");
                foreach (var proc in explorerProcesses)
                {
                    try
                    {
                        proc.Kill();
                    }
                    catch { }
                }

                // Start lại Explorer
                await Task.Delay(1000);
                Process.Start("explorer.exe");

                UpdateStatus($"Đã chuyển sang {(isDarkTheme ? "Light" : "Dark")} Theme và restart Explorer", "Green");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi đổi theme: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Event handler khi click vào Theme Toggle Button
        /// </summary>
        private async void TglTheme_Click(object sender, RoutedEventArgs e)
        {
            await ToggleThemeAsync();
            
            // Cập nhật lại trạng thái toggle button sau khi đổi theme
            await Task.Delay(1500);
            UpdateThemeToggleButtonState();
        }

        /// <summary>
        /// Cập nhật trạng thái của Theme Toggle Button dựa trên theme hiện tại
        /// </summary>
        private void UpdateThemeToggleButtonState()
        {
            try
            {
                if (TglTheme != null)
                {
                    bool isDarkTheme = IsDarkThemeEnabled();
                    TglTheme.IsChecked = isDarkTheme;
                }
            }
            catch { }
        }

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


        private void ChkInstallNeatDM_Click(object sender, RoutedEventArgs e)
        {
            if (ChkInstallNeatDM.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Neat Download Manager", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Neat Download Manager", "Yellow");
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


        private void ChkInstallZalo_Click(object sender, RoutedEventArgs e)
        {
            if (ChkInstallZalo.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Zalo", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Zalo", "Yellow");
            }

            UpdateInstallButtonState();
        }

    }
}
