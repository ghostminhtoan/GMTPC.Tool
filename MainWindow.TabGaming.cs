// =======================================================================
// AI Summary:
// Date: 2026-03-11
// - Added InstallGhostOfTsushimaAsync() method for Ghost of Tsushima (29 parts)
// - Added FolderBrowserDialog for selecting temp download location
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
        private async Task InstallMSIAfterburnerAsync()
        {
            try
            {
                UpdateStatus("Đang tải MSI Afterburner...", "Cyan");
                string msiAfterburnerPath = Path.Combine(GetGMTPCFolder(), "MSIAfterburner.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/MSI.Afterburner.exe", msiAfterburnerPath, "MSI Afterburner");

                UpdateStatus("Đang cài đặt MSI Afterburner...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = msiAfterburnerPath,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt MSI Afterburner hoàn tất!", "Green");
                }

                if (File.Exists(msiAfterburnerPath))
                {
                    File.Delete(msiAfterburnerPath);
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

        private void ChkProcessLasso_Click(object sender, RoutedEventArgs e)
        {
            if (ChkProcessLasso.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Process Lasso", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Process Lasso", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkThrottlestop_Click(object sender, RoutedEventArgs e)
        {
            if (ChkThrottlestop.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Throttlestop", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Throttlestop", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkMSIAfterburner_Click(object sender, RoutedEventArgs e)
        {
            if (ChkMSIAfterburner.IsChecked == true)
            {
                UpdateStatus("Đã chọn: MSI Afterburner", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: MSI Afterburner", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkLeagueOfLegends_Click(object sender, RoutedEventArgs e)
        {
            if (ChkLeagueOfLegends.IsChecked == true)
            {
                UpdateStatus("Đã chọn: League of Legends VN", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: League of Legends VN", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkPorofessor_Click(object sender, RoutedEventArgs e)
        {
            if (ChkPorofessor.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Porofessor", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Porofessor", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkSamuraiMaiden_Click(object sender, RoutedEventArgs e)
        {
            if (ChkSamuraiMaiden.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Samurai Maiden", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Samurai Maiden", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkGhostOfTsushima_Click(object sender, RoutedEventArgs e)
        {
            if (ChkGhostOfTsushima.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Ghost of Tsushima", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Ghost of Tsushima", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private async Task InstallProcessLassoAsync()
        {
            try
            {
                UpdateStatus("Đang tải Process Lasso...", "Cyan");
                string processLassoPath = Path.Combine(GetGMTPCFolder(), "ProcessLasso.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Process.Lasso.exe", processLassoPath, "Process Lasso");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                MessageBoxResult result = MessageBox.Show("Yes = Cài đặt tự động vào ổ C\nNo = Cài vào ổ khác", "Cài đặt Process Lasso", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = processLassoPath,
                    UseShellExecute = true
                };

                if (result == MessageBoxResult.Yes)
                {
                    startInfo.Arguments = "/s";
                    UpdateStatus("Cài đặt Process Lasso vào ổ C...", "Yellow");
                }
                else if (result == MessageBoxResult.No)
                {
                    UpdateStatus("Cài Process Lasso vào ổ khác...", "Yellow");
                }
                else
                {
                    UpdateStatus("Đã hủy cài đặt Process Lasso", "Yellow");
                    if (File.Exists(processLassoPath))
                    {
                        File.Delete(processLassoPath);
                    }
                    return;
                }

                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt Process Lasso hoàn tất!", "Green");
                }

                if (File.Exists(processLassoPath))
                {
                    File.Delete(processLassoPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Process Lasso: {ex.Message}", "Red");
            }
        }


        private async Task InstallThrottlestopAsync()
        {
            try
            {
                UpdateStatus("Đang tải Throttlestop...", "Cyan");
                string throttlestopPath = Path.Combine(GetGMTPCFolder(), "Throttlestop.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Throttlestop.exe", throttlestopPath, "Throttlestop");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                MessageBoxResult result = MessageBox.Show("Yes = Cài đặt tự động vào ổ C\nNo = Cài vào ổ khác", "Cài đặt Throttlestop", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = throttlestopPath,
                    UseShellExecute = true
                };

                if (result == MessageBoxResult.Yes)
                {
                    startInfo.Arguments = "/s";
                    UpdateStatus("Cài đặt Throttlestop vào ổ C...", "Yellow");
                }
                else if (result == MessageBoxResult.No)
                {
                    UpdateStatus("Cài Throttlestop vào ổ khác...", "Yellow");
                }
                else
                {
                    UpdateStatus("Đã hủy cài đặt Throttlestop", "Yellow");
                    if (File.Exists(throttlestopPath))
                    {
                        File.Delete(throttlestopPath);
                    }
                    return;
                }

                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt Throttlestop hoàn tất!", "Green");
                }

                if (File.Exists(throttlestopPath))
                {
                    File.Delete(throttlestopPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Throttlestop: {ex.Message}", "Red");
            }
        }


        private async Task InstallLeagueOfLegendsVNAsync()
        {
            try
            {
                UpdateStatus("Đang tải League of Legends VN...", "Cyan");
                string lolPath = Path.Combine(GetGMTPCFolder(), "LeagueOfLegendsVN.exe");
                await DownloadWithProgressAsync("https://lol.secure.dyn.riotcdn.net/channels/public/x/installer/current/live.vn2.exe", lolPath, "League of Legends VN");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang cài đặt League of Legends VN...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = lolPath,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Cài đặt League of Legends VN hoàn tất!", "Green");
                }

                if (File.Exists(lolPath))
                {
                    File.Delete(lolPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt League of Legends VN: {ex.Message}", "Red");
            }
        }


        private async Task InstallPorofessorAsync()
        {
            try
            {
                UpdateStatus("Đang tải Porofessor...", "Cyan");
                string porofessorPath = Path.Combine(GetGMTPCFolder(), "Porofessor.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Porofessor.exe", porofessorPath, "Porofessor");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Porofessor...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = porofessorPath,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Porofessor đã hoàn tất!", "Green");
                }

                if (File.Exists(porofessorPath))
                {
                    File.Delete(porofessorPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Porofessor: {ex.Message}", "Red");
            }
        }


        private async Task InstallSamuraiMaidenAsync()
        {
            try
            {
                string gmtFolder = GetGMTPCFolder();
                string part1Path = Path.Combine(gmtFolder, "SAMURAI.MAIDEN_LinkNeverDie.Com.part1.exe");
                string part2Path = Path.Combine(gmtFolder, "SAMURAI.MAIDEN_LinkNeverDie.Com.part2.rar");
                string part3Path = Path.Combine(gmtFolder, "SAMURAI.MAIDEN_LinkNeverDie.Com.part3.rar");
                string part4Path = Path.Combine(gmtFolder, "SAMURAI.MAIDEN_LinkNeverDie.Com.part4.rar");

                // Download part 1
                UpdateStatus("Đang tải Samurai Maiden - Part 1...", "Cyan");
                await DownloadWithProgressAsync(
                    "https://github.com/ghostminhtoan/MMT/releases/download/game/SAMURAI.MAIDEN_LinkNeverDie.Com.part1.exe",
                    part1Path, "Samurai Maiden - Part 1");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Download part 2
                UpdateStatus("Đang tải Samurai Maiden - Part 2...", "Cyan");
                await DownloadWithProgressAsync(
                    "https://github.com/ghostminhtoan/MMT/releases/download/game/SAMURAI.MAIDEN_LinkNeverDie.Com.part2.rar",
                    part2Path, "Samurai Maiden - Part 2");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Download part 3
                UpdateStatus("Đang tải Samurai Maiden - Part 3...", "Cyan");
                await DownloadWithProgressAsync(
                    "https://github.com/ghostminhtoan/MMT/releases/download/game/SAMURAI.MAIDEN_LinkNeverDie.Com.part3.rar",
                    part3Path, "Samurai Maiden - Part 3");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Download part 4
                UpdateStatus("Đang tải Samurai Maiden - Part 4...", "Cyan");
                await DownloadWithProgressAsync(
                    "https://github.com/ghostminhtoan/MMT/releases/download/game/SAMURAI.MAIDEN_LinkNeverDie.Com.part4.rar",
                    part4Path, "Samurai Maiden - Part 4");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Run part1.exe
                UpdateStatus("Đang chạy SAMURAI.MAIDEN_LinkNeverDie.Com.part1.exe...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = part1Path,
                    UseShellExecute = true,
                    WorkingDirectory = gmtFolder
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Samurai Maiden đã hoàn tất!", "Green");
                }

                // Delete all parts after installation
                if (File.Exists(part1Path)) File.Delete(part1Path);
                if (File.Exists(part2Path)) File.Delete(part2Path);
                if (File.Exists(part3Path)) File.Delete(part3Path);
                if (File.Exists(part4Path)) File.Delete(part4Path);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Samurai Maiden: {ex.Message}", "Red");
            }
        }

        private async Task InstallGhostOfTsushimaAsync()
        {
            try
            {
                // Use the auto-selected temp path from drive ComboBox
                // Format: {DriveLetter}:\temp (e.g., K:\temp)
                string tempFolder = GetSelectedTempPath();
                
                if (string.IsNullOrEmpty(tempFolder))
                {
                    UpdateStatus("Error: Cannot create temp folder. Select different drive.", "Red");
                    return;
                }

                UpdateStatus($"Downloading to: {tempFolder}", "Cyan");

                // Create Ghost of Tsushima subfolder in temp
                string gotFolder = Path.Combine(tempFolder, "GhostOfTsushima");
                if (!Directory.Exists(gotFolder))
                {
                    Directory.CreateDirectory(gotFolder);
                }

                string part01Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part01.exe");
                string part02Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part02.rar");
                string part03Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part03.rar");
                string part04Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part04.rar");
                string part05Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part05.rar");
                string part06Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part06.rar");
                string part07Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part07.rar");
                string part08Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part08.rar");
                string part09Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part09.rar");
                string part10Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part10.rar");
                string part11Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part11.rar");
                string part12Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part12.rar");
                string part13Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part13.rar");
                string part14Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part14.rar");
                string part15Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part15.rar");
                string part16Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part16.rar");
                string part17Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part17.rar");
                string part18Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part18.rar");
                string part19Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part19.rar");
                string part20Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part20.rar");
                string part21Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part21.rar");
                string part22Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part22.rar");
                string part23Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part23.rar");
                string part24Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part24.rar");
                string part25Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part25.rar");
                string part26Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part26.rar");
                string part27Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part27.rar");
                string part28Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part28.rar");
                string part29Path = Path.Combine(gotFolder, "Ghost.of.Tsushima_LinkNeverDie.Com.part29.rar");

                // Download all 29 parts
                UpdateStatus("Đang tải Ghost of Tsushima - Part 1/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART01_URL, part01Path, "Ghost of Tsushima - Part 1");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 2/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART02_URL, part02Path, "Ghost of Tsushima - Part 2");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 3/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART03_URL, part03Path, "Ghost of Tsushima - Part 3");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 4/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART04_URL, part04Path, "Ghost of Tsushima - Part 4");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 5/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART05_URL, part05Path, "Ghost of Tsushima - Part 5");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 6/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART06_URL, part06Path, "Ghost of Tsushima - Part 6");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 7/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART07_URL, part07Path, "Ghost of Tsushima - Part 7");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 8/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART08_URL, part08Path, "Ghost of Tsushima - Part 8");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 9/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART09_URL, part09Path, "Ghost of Tsushima - Part 9");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 10/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART10_URL, part10Path, "Ghost of Tsushima - Part 10");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 11/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART11_URL, part11Path, "Ghost of Tsushima - Part 11");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 12/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART12_URL, part12Path, "Ghost of Tsushima - Part 12");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 13/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART13_URL, part13Path, "Ghost of Tsushima - Part 13");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 14/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART14_URL, part14Path, "Ghost of Tsushima - Part 14");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 15/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART15_URL, part15Path, "Ghost of Tsushima - Part 15");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 16/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART16_URL, part16Path, "Ghost of Tsushima - Part 16");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 17/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART17_URL, part17Path, "Ghost of Tsushima - Part 17");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 18/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART18_URL, part18Path, "Ghost of Tsushima - Part 18");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 19/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART19_URL, part19Path, "Ghost of Tsushima - Part 19");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 20/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART20_URL, part20Path, "Ghost of Tsushima - Part 20");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 21/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART21_URL, part21Path, "Ghost of Tsushima - Part 21");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 22/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART22_URL, part22Path, "Ghost of Tsushima - Part 22");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 23/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART23_URL, part23Path, "Ghost of Tsushima - Part 23");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 24/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART24_URL, part24Path, "Ghost of Tsushima - Part 24");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 25/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART25_URL, part25Path, "Ghost of Tsushima - Part 25");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 26/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART26_URL, part26Path, "Ghost of Tsushima - Part 26");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 27/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART27_URL, part27Path, "Ghost of Tsushima - Part 27");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 28/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART28_URL, part28Path, "Ghost of Tsushima - Part 28");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                UpdateStatus("Đang tải Ghost of Tsushima - Part 29/29...", "Cyan");
                await DownloadWithProgressAsync(GHOST_OF_TSUSHIMA_PART29_URL, part29Path, "Ghost of Tsushima - Part 29");
                Dispatcher.Invoke(() => { DownloadProgressBar.Value = 0; ProgressTextBlock.Text = ""; SpeedTextBlock.Text = ""; });

                // Run part01.exe
                UpdateStatus("Đang chạy Ghost.of.Tsushima_LinkNeverDie.Com.part01.exe...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = part01Path,
                    UseShellExecute = true,
                    WorkingDirectory = tempFolder
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Ghost of Tsushima đã hoàn tất!", "Green");
                }

                // Note: Not deleting temp folder - user may want to keep files
                UpdateStatus("Installation complete. Files remain in \\temp.", "Green");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Ghost of Tsushima: {ex.Message}", "Red");
            }
        }

    }
}
