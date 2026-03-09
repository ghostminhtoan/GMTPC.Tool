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

/*
 * AI Summary:
 * Date: 2026-03-09
 * - Updated ChkAdvancedCodec with new link and argument
 */

namespace GMTPC.Tool
{
    public partial class MainWindow
    {

        private async Task InstallPotPlayerAsync()
        {
            try
            {
                UpdateStatus("Đang tải PotPlayer...", "Cyan");
                string potPath = Path.Combine(GetGMTPCFolder(), "PotPlayerSetup64.exe");
                await DownloadWithProgressAsync("https://t1.daumcdn.net/potplayer/PotPlayer/Version/Latest/PotPlayerSetup64.exe", potPath, "PotPlayer");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy PotPlayer installer (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = potPath,
                    Arguments = "/S",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("PotPlayer đã hoàn tất.", "Green");
                }

                if (File.Exists(potPath)) File.Delete(potPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài PotPlayer: {ex.Message}", "Red");
            }
        }


        private async Task InstallFastStoneAsync()
        {
            try
            {
                UpdateStatus("Đang tải FastStone Capture...", "Cyan");
                string fsPath = Path.Combine(GetGMTPCFolder(), "FastStone.Capture.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/FastStone.Capture.exe", fsPath, "FastStone Capture");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy FastStone Capture installer (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fsPath,
                    Arguments = "/silent",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("FastStone Capture đã hoàn tất.", "Green");
                }

                if (File.Exists(fsPath)) File.Delete(fsPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài FastStone Capture: {ex.Message}", "Red");
            }
        }


        private async Task InstallFoxitAsync()
        {
            try
            {
                UpdateStatus("Đang tải Foxit PDF Reader...", "Cyan");
                string foxitPath = Path.Combine(GetGMTPCFolder(), "FoxitPDFReaderSetup.exe");
                await DownloadWithProgressAsync("https://cdn01.foxitsoftware.com/product/reader/desktop/win/2025.2.0/FoxitPDFReader20252_L10N_Setup_Prom_x64.exe", foxitPath, "Foxit PDF Reader");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Foxit installer (quiet)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = foxitPath,
                    Arguments = "/quiet",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Foxit đã cài xong, đang khởi chạy Foxit...", "Green");
                }

                // Run Foxit after install
                string foxitExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Foxit Software", "Foxit PDF Reader", "foxitPDFReader.exe");
                try
                {
                    if (File.Exists(foxitExe))
                    {
                        Process.Start(foxitExe);
                        MessageBox.Show("Nếu thấy chữ 'register' thì chọn 'Not now', sau đó ấn 'Next' liên tục để hoàn tất.", "Lưu ý", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        UpdateStatus("Không tìm thấy file foxitPDFReader.exe để khởi chạy sau cài đặt.", "Yellow");
                    }
                }
                catch { }

                if (File.Exists(foxitPath)) File.Delete(foxitPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài Foxit PDF Reader: {ex.Message}", "Red");
            }
        }


        private async Task InstallBandiviewAsync()
        {
            try
            {
                UpdateStatus("Đang tải Bandiview...", "Cyan");
                string bPath = Path.Combine(GetGMTPCFolder(), "Bandiview.exe");
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/Bandiview.exe", bPath, "Bandiview");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Bandiview installer (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = bPath,
                    Arguments = "/silent",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Bandiview đã hoàn tất.", "Green");
                }

                if (File.Exists(bPath)) File.Delete(bPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài Bandiview: {ex.Message}", "Red");
            }
        }


        private async Task InstallAdvancedCodecAsync()
        {
            try
            {
                UpdateStatus("Đang tải Advanced Codec Pack...", "Cyan");
                string codecPath = Path.Combine(GetGMTPCFolder(), "ADVANCED_Codec_Pack.exe");
                await DownloadWithProgressAsync("https://github.com/github.com/ghostminhtoan/MMT/releases/download/v1.0/ADVANCED_Codec_Pack.exe", codecPath, "Advanced Codec Pack");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                UpdateStatus("Đang chạy Advanced Codec installer (silent)...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = codecPath,
                    Arguments = "/S /v/qn",
                    UseShellExecute = true
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    UpdateStatus("Advanced Codec Pack đã hoàn tất.", "Green");
                }

                if (File.Exists(codecPath)) File.Delete(codecPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài Advanced Codec Pack: {ex.Message}", "Red");
            }
        }


        private void ChkPotPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (ChkPotPlayer.IsChecked == true)
            {
                UpdateStatus("Đã chọn: PotPlayer", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: PotPlayer", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkFastStone_Click(object sender, RoutedEventArgs e)
        {
            if (ChkFastStone.IsChecked == true)
            {
                UpdateStatus("Đã chọn: FastStone Capture", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: FastStone Capture", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkFoxit_Click(object sender, RoutedEventArgs e)
        {
            if (ChkFoxit.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Foxit PDF Reader", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Foxit PDF Reader", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkBandiview_Click(object sender, RoutedEventArgs e)
        {
            if (ChkBandiview.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Bandiview (Picture viewer)", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Bandiview (Picture viewer)", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkAdvancedCodec_Click(object sender, RoutedEventArgs e)
        {
            if (ChkAdvancedCodec.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Advanced Codec Pack (Video Music codec)", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Advanced Codec Pack (Video Music codec)", "Yellow");
            }

            UpdateInstallButtonState();
        }

    }
}
