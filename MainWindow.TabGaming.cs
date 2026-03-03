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

    }
}
