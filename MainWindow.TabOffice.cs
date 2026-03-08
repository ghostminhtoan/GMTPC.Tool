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
        /*
         * AI Summary:
         * Date: 2026-03-08
         * - Added ChkGouenjiFonts_Click and InstallGouenjiFontsAsync
         * - Added ChkNotepadPlusPlus_Click and InstallNotepadPlusPlusAsync
         */

        private async void BtnActivateOffice_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đã chọn: Tự động kích hoạt Office", "Green");
            await Task.Run(() => ActivateOffice());
        }


        // ===================== Chức năng kích hoạt Office =====================
        private void ActivateOffice()
        {
            try
            {
                UpdateStatus("Đang kích hoạt Office...", "Cyan");
                string activateOfficeCmdPath = Path.Combine(GetGMTPCFolder(), "ACTIVATE.OFFICE.cmd");

                // Tải file ACTIVATE.OFFICE.cmd
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile("https://github.com/ghostminhtoan/MMT/releases/download/activate/ACTIVATE.OFFICE.cmd", activateOfficeCmdPath);
                }
                UpdateStatus("Đã tải file ACTIVATE.OFFICE.cmd", "Cyan");

                // Chạy script với quyền admin
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = activateOfficeCmdPath,
                    UseShellExecute = true,
                    Verb = "runas"
                };

                Process.Start(startInfo);
                UpdateStatus("Đã mở cửa sổ kích hoạt Office", "Green");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi kích hoạt Office: {ex.Message}", "Red");
            }
        }


        private void ChkActivateOffice_Click(object sender, RoutedEventArgs e)
        {
            if (ChkActivateOffice.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Tự động kích hoạt Office", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Tự động kích hoạt Office", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkOfficeToolPlus_Click(object sender, RoutedEventArgs e)
        {
            if (ChkOfficeToolPlus.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Office Tool Plus", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Office Tool Plus", "Yellow");
            }

            UpdateInstallButtonState();
        }


        // Helper methods for installation tasks to be called from BtnInstall_Click
        private async Task InstallOfficeToolPlusAsync()
        {
            try
            {
                UpdateStatus("Đang tải Office Tool Plus...", "Cyan");
                string officeToolPlusPath = Path.Combine(GetGMTPCFolder(), "office.tool.plus.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/office.tool.plus.exe", officeToolPlusPath, "Office Tool Plus Installer");

                // Đảm bảo progress bar reset sau khi tải
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Office Tool Plus installer với lệnh /s...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = officeToolPlusPath,
                    Arguments = "/s", // Lệnh /s cho chế độ silent
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    if (process.ExitCode == 0)
                    {
                        UpdateStatus("Cài đặt Office Tool Plus thành công!", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Cài đặt Office Tool Plus thất bại. Mã lỗi: {process.ExitCode}", "Red");
                    }
                }
                // Xóa file installer sau khi chạy xong
                if (File.Exists(officeToolPlusPath))
                {
                    File.Delete(officeToolPlusPath);
                    UpdateStatus("Đã xóa file Office Tool Plus installer tạm thời", "Cyan");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải hoặc cài đặt Office Tool Plus: {ex.Message}", "Red");
            }
        }


        // Thêm phương thức xử lý sự kiện Click cho Office Softmaker
        private void ChkOfficeSoftmaker_Click(object sender, RoutedEventArgs e)
        {
            if (ChkOfficeSoftmaker.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Office Softmaker", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Office Softmaker", "Yellow");
            }

            UpdateInstallButtonState();
        }


        // Thêm phương thức cài đặt Office Softmaker
        private async Task InstallOfficeSoftmakerAsync()
        {
            try
            {
                UpdateStatus("Đang tải Office Softmaker...", "Cyan");
                string officeSoftmakerPath = Path.Combine(GetGMTPCFolder(), "Office.Softmaker.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Office.Softmaker.exe", officeSoftmakerPath, "Office Softmaker Installer");

                // Đảm bảo progress bar reset sau khi tải
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Hiển thị popup để hỏi người dùng chọn cài đặt
                MessageBoxResult result = MessageBox.Show("Yes = Cài đặt tự động vào ổ C\nNo = Cài vào ổ khác", "Cài đặt tự động Office Softmaker", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = officeSoftmakerPath,
                    UseShellExecute = true
                };

                if (result == MessageBoxResult.Yes) // Cài đặt tự động vào ổ C
                {
                    startInfo.Arguments = "/passive"; // Sử dụng /passive như yêu cầu
                    UpdateStatus("1 = Cài đặt tự động vào ổ C", "Yellow");
                }
                else if (result == MessageBoxResult.No) // Cài vào ổ khác
                {
                    UpdateStatus("2 = Cài vào ổ khác", "Yellow");
                }
                else // Hủy
                {
                    UpdateStatus("Đã hủy cài đặt Office Softmaker", "Yellow");
                    if (File.Exists(officeSoftmakerPath))
                    {
                        File.Delete(officeSoftmakerPath);
                    }
                    return;
                }

                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());

                    if (process.ExitCode == 0)
                    {
                        UpdateStatus("Cài đặt Office Softmaker thành công!", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Cài đặt Office Softmaker thất bại. Mã lỗi: {process.ExitCode}", "Red");
                    }
                }

                // Xóa file sau khi cài đặt xong
                if (File.Exists(officeSoftmakerPath))
                {
                    File.Delete(officeSoftmakerPath);
                    UpdateStatus("Đã xóa file Office.Softmaker.exe", "Cyan");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Office Softmaker: {ex.Message}", "Red");
            }
        }

        // ===================================================================
        // TabOffice — Gouenji Fansub Fonts
        // ===================================================================
        private void ChkGouenjiFonts_Click(object sender, RoutedEventArgs e)
        {
            if (ChkGouenjiFonts.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Gouenji Fansub Fonts", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Gouenji Fansub Fonts", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private async Task InstallGouenjiFontsAsync()
        {
            try
            {
                UpdateStatus("Đang tải Gouenji Fansub Fonts...", "Cyan");
                string fontsPath = Path.Combine(GetGMTPCFolder(), "Gouenji.Fansub.Fonts.exe");
                await DownloadWithProgressAsync(GOUENJI_FONTS_DOWNLOAD_URL, fontsPath, "Gouenji Fansub Fonts");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt Gouenji Fansub Fonts (passive)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fontsPath,
                    Arguments = GOUENJI_FONTS_INSTALL_ARGUMENTS,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt Gouenji Fansub Fonts hoàn tất!", "Green");
                }

                if (File.Exists(fontsPath))
                {
                    File.Delete(fontsPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Gouenji Fansub Fonts: {ex.Message}", "Red");
            }
        }

        // ===================================================================
        // TabOffice — Notepad++
        // ===================================================================
        private void ChkNotepadPlusPlus_Click(object sender, RoutedEventArgs e)
        {
            if (ChkNotepadPlusPlus.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Notepad++", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Notepad++", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private async Task InstallNotepadPlusPlusAsync()
        {
            try
            {
                UpdateStatus("Đang tải Notepad++...", "Cyan");
                string notepadPlusPlusPath = Path.Combine(GetGMTPCFolder(), "npp.8.9.2.Installer.exe");
                await DownloadWithProgressAsync(NOTEPAD_PLUS_PLUS_DOWNLOAD_URL, notepadPlusPlusPath, "Notepad++");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt Notepad++ (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = notepadPlusPlusPath,
                    Arguments = NOTEPAD_PLUS_PLUS_INSTALL_ARGUMENTS,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt Notepad++ hoàn tất!", "Green");
                }

                if (File.Exists(notepadPlusPlusPath))
                {
                    File.Delete(notepadPlusPlusPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Notepad++: {ex.Message}", "Red");
            }
        }

    }
}
