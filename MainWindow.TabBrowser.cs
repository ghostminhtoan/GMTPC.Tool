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

        private async Task InstallCocCocAsync()
        {
            UpdateStatus("Đang tải Cốc Cốc...", "Cyan");
            string cocCocInstallerPath = Path.Combine(GetGMTPCFolder(), "coccoc_standalone_vi.exe");
            try
            {
                await DownloadWithProgressAsync(COCCOC_DOWNLOAD_URL, cocCocInstallerPath, "Cốc Cốc");

                // Reset progress UI after download
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Cốc Cốc installer ( " + COCCOC_INSTALL_ARGUMENTS + " )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = cocCocInstallerPath,
                    Arguments = COCCOC_INSTALL_ARGUMENTS,
                    UseShellExecute = true
                };

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    if (process.ExitCode == 0)
                    {
                        UpdateStatus("Cài đặt Cốc Cốc hoàn tất.", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Cốc Cốc installer kết thúc với mã {process.ExitCode}", "Yellow");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Tải Cốc Cốc bị hủy.", "Red");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải hoặc cài đặt Cốc Cốc: {ex.Message}", "Red");
            }
            finally
            {
                if (File.Exists(cocCocInstallerPath))
                {
                    try { File.Delete(cocCocInstallerPath); }
                    catch { }
                }
            }
        }


        private async Task InstallEdgeAsync()
        {
            try
            {
                UpdateStatus("Đang tải Microsoft Edge...", "Cyan");
                string edgePath = Path.Combine(GetGMTPCFolder(), "MicrosoftEdgeSetup.exe");
                await DownloadWithProgressAsync(EDGE_DOWNLOAD_URL, edgePath, "Microsoft Edge");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Microsoft Edge installer ( " + EDGE_INSTALL_ARGUMENTS + " )...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = edgePath,
                    Arguments = EDGE_INSTALL_ARGUMENTS,
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Microsoft Edge đã hoàn tất.", "Green");
                }

                if (File.Exists(edgePath)) File.Delete(edgePath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài Microsoft Edge: {ex.Message}", "Red");
            }
        }


        private void ChkChrome_Click(object sender, RoutedEventArgs e)
        {
            if (ChkChrome.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Google Chrome", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Google Chrome", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkCocCoc_Click(object sender, RoutedEventArgs e)
        {
            if (ChkCocCoc.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Cốc Cốc", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Cốc Cốc", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkEdge_Click(object sender, RoutedEventArgs e)
        {
            if (ChkEdge.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Microsoft Edge", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Microsoft Edge", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private async Task InstallChromeAsync()
        {
            UpdateStatus("Đang tải Google Chrome...", "Cyan");
            string chromeInstallerPath = Path.Combine(GetGMTPCFolder(), "ChromeSetup.exe");
            try
            {
                await DownloadWithProgressAsync(CHROME_DOWNLOAD_URL, chromeInstallerPath, "Google Chrome");

                // Reset progress UI after download
                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Chrome installer ( " + CHROME_INSTALL_ARGUMENTS + " )...", "Yellow");

                // Run installer with arguments
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = chromeInstallerPath,
                    Arguments = CHROME_INSTALL_ARGUMENTS,
                    UseShellExecute = true
                };

                Process process = Process.Start(startInfo);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    if (process.ExitCode == 0)
                    {
                        UpdateStatus("Cài đặt Google Chrome hoàn tất.", "Green");
                    }
                    else
                    {
                        UpdateStatus($"Chrome installer kết thúc với mã {process.ExitCode}", "Yellow");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Tải Chrome bị hủy.", "Red");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tải hoặc cài đặt Chrome: {ex.Message}", "Red");
            }
            finally
            {
                if (File.Exists(chromeInstallerPath))
                {
                    try { File.Delete(chromeInstallerPath); }
                    catch { }
                }
            }
        }

    }
}
