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

        private void ChkUltraviewer_Click(object sender, RoutedEventArgs e)
        {
            if (ChkUltraviewer.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Ultraviewer", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Ultraviewer", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkTeamViewerQS_Click(object sender, RoutedEventArgs e)
        {
            if (ChkTeamViewerQS.IsChecked == true)
            {
                UpdateStatus("Đã chọn: TeamViewer QuickSupport", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: TeamViewer QuickSupport", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkTeamViewerFull_Click(object sender, RoutedEventArgs e)
        {
            if (ChkTeamViewerFull.IsChecked == true)
            {
                UpdateStatus("Đã chọn: TeamViewer Full", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: TeamViewer Full", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkAnyDesk_Click(object sender, RoutedEventArgs e)
        {
            if (ChkAnyDesk.IsChecked == true)
            {
                UpdateStatus("Đã chọn: AnyDesk", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: AnyDesk", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private async Task InstallUltraviewerAsync()
        {
            try
            {
                UpdateStatus("Đang tải Ultraviewer...", "Cyan");
                string ultraviewerPath = Path.Combine(GetGMTPCFolder(), "UltraViewer_setup.exe");
                await DownloadWithProgressAsync("https://dl2.ultraviewer.net/UltraViewer_setup_6.6_vi.exe", ultraviewerPath, "Ultraviewer");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Ultraviewer...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = ultraviewerPath,
                    Arguments = "/silent",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Ultraviewer đã hoàn tất!", "Green");
                }

                if (File.Exists(ultraviewerPath))
                {
                    File.Delete(ultraviewerPath);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt Ultraviewer: {ex.Message}", "Red");
            }
        }


        private async Task InstallTeamViewerQuickSupportAsync()
        {
            try
            {
                UpdateStatus("Đang tải TeamViewer QuickSupport...", "Cyan");
                string tvqsPath = Path.Combine(GetGMTPCFolder(), "TeamViewerQS_x64.exe");
                await DownloadWithProgressAsync("https://dl.teamviewer.com/download/TeamViewerQS_x64.exe", tvqsPath, "TeamViewer QuickSupport");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy TeamViewer QuickSupport...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = tvqsPath,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("TeamViewer QuickSupport đã hoàn tất.", "Green");
                }

                if (File.Exists(tvqsPath)) File.Delete(tvqsPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài TeamViewer QuickSupport: {ex.Message}", "Red");
            }
        }


        private async Task InstallTeamViewerFullAsync()
        {
            try
            {
                UpdateStatus("Đang tải TeamViewer Full...", "Cyan");
                string tvFullPath = Path.Combine(GetGMTPCFolder(), "TeamViewer_Setup.exe");
                await DownloadWithProgressAsync("https://tinyurl.com/teamviewerlatest", tvFullPath, "TeamViewer Full");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy TeamViewer Full (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = tvFullPath,
                    Arguments = "/S /V/qn",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("TeamViewer Full đã hoàn tất.", "Green");
                }

                if (File.Exists(tvFullPath)) File.Delete(tvFullPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài TeamViewer Full: {ex.Message}", "Red");
            }
        }


        private async Task InstallTeraCopyAsync()
        {
            try
            {
                UpdateStatus("Đang tải TeraCopy...", "Cyan");
                string teracopyPath = Path.Combine(GetGMTPCFolder(), "teracopy.exe");
                await DownloadWithProgressAsync("https://www.codesector.com/files/teracopy.exe", teracopyPath, "TeraCopy");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy TeraCopy (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = teracopyPath,
                    Arguments = "/S",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("TeraCopy đã hoàn tất.", "Green");
                }

                if (File.Exists(teracopyPath)) File.Delete(teracopyPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài TeraCopy: {ex.Message}", "Red");
            }
        }


        private async Task InstallAnyDeskAsync()
        {
            try
            {
                UpdateStatus("Đang tải AnyDesk...", "Cyan");
                string anydeskPath = Path.Combine(GetGMTPCFolder(), "AnyDesk.exe");
                await DownloadWithProgressAsync("https://tinyurl.com/anydesk621", anydeskPath, "AnyDesk");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy AnyDesk...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = anydeskPath,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("AnyDesk đã hoàn tất.", "Green");
                }

                if (File.Exists(anydeskPath)) File.Delete(anydeskPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài AnyDesk: {ex.Message}", "Red");
            }
        }


        private void ChkVMWare162Lite_Click(object sender, RoutedEventArgs e)
        {
            if (ChkVMWare162Lite.IsChecked == true)
            {
                UpdateStatus("Đã chọn: VMWare 16.2 lite", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: VMWare 16.2 lite", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private async Task InstallVMWare162LiteAsync()
        {
            try
            {
                UpdateStatus("Đang tải VMWare 16.2 lite...", "Cyan");
                string vmwarePath = Path.Combine(GetGMTPCFolder(), "VMware_Workstation_16.2.2_Lite.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/VMware_Workstation_16.2.2_Lite_Eng_._Rus.exe", vmwarePath, "VMWare 16.2 lite");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy VMWare 16.2 lite (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = vmwarePath,
                    Arguments = "/S /V/qn",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("VMWare 16.2 lite đã hoàn tất.", "Green");
                }

                if (File.Exists(vmwarePath)) File.Delete(vmwarePath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài VMWare 16.2 lite: {ex.Message}", "Red");
            }
        }

    }
}
