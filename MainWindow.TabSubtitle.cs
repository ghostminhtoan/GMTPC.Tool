using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
         * Date: 2026-03-29
         * - Created MainWindow.TabSubtitle.cs for Subtitle tab
         * - Added ChkVidCoder_Click, InstallVidCoderAsync with GitHub latest version probe
         * Note: ChkSubtitleEdit_Click and InstallSubtitleEditAsync remain in MainWindow.TabOffice.cs
         */

        // ===================================================================
        // TabSubtitle — VidCoder
        // ===================================================================
        private void ChkVidCoder_Click(object sender, RoutedEventArgs e)
        {
            if (ChkVidCoder.IsChecked == true)
            {
                UpdateStatus("Đã chọn: VidCoder", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: VidCoder", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private async Task InstallVidCoderAsync()
        {
            try
            {
                // Bước 1: Tạo folder C:\Vidcoder nếu chưa tồn tại
                string vidCoderFolder = @"C:\Vidcoder";
                if (!Directory.Exists(vidCoderFolder))
                {
                    Directory.CreateDirectory(vidCoderFolder);
                    UpdateStatus($"Đã tạo folder {vidCoderFolder}", "Cyan");
                }

                // Bước 2: Tìm phiên bản VidCoder mới nhất từ GitHub
                UpdateStatus("Đang tìm phiên bản VidCoder mới nhất...", "Cyan");
                string latestVersion = await GetLatestVidCoderVersionAsync();
                
                if (string.IsNullOrEmpty(latestVersion))
                {
                    UpdateStatus("Không thể tìm thấy phiên bản VidCoder mới nhất!", "Red");
                    return;
                }

                UpdateStatus($"Phiên bản mới nhất: {latestVersion}", "Green");

                // Bước 3: Tải VidCoder.exe
                string vidCoderExeUrl = $"https://github.com/RandomEngy/VidCoder/releases/download/{latestVersion}/VidCoder-{latestVersion.TrimStart('v')}-Portable.exe";
                string vidCoderExePath = Path.Combine(vidCoderFolder, "VidCoder.exe");
                
                UpdateStatus($"Đang tải VidCoder {latestVersion}...", "Cyan");
                await DownloadWithProgressAsync(vidCoderExeUrl, vidCoderExePath, "VidCoder");

                Dispatcher.Invoke(() =>
                {
                    DownloadProgressBar.Value = 0;
                    ProgressTextBlock.Text = "";
                    SpeedTextBlock.Text = "";
                });

                // Bước 4: Tải file VidCoder.sqlite từ MMT repo
                string vidCoderSqliteUrl = "https://github.com/ghostminhtoan/MMT/releases/download/v1.0/VidCoder.sqlite";
                string vidCoderSqlitePath = Path.Combine(vidCoderFolder, "VidCoder.sqlite");

                UpdateStatus("Đang tải VidCoder.sqlite...", "Cyan");
                using (WebClient client = new WebClient())
                {
                    await client.DownloadFileTaskAsync(vidCoderSqliteUrl, vidCoderSqlitePath);
                }

                UpdateStatus("Đã tải xong VidCoder.sqlite", "Green");

                // Bước 5: Chỉ chạy file .exe sau khi tải xong SQLite
                UpdateStatus("Đang mở VidCoder...", "Cyan");
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = vidCoderExePath,
                    UseShellExecute = true,
                    WorkingDirectory = vidCoderFolder
                };
                Process process = Process.Start(startInfo);

                if (process != null)
                {
                    UpdateStatus("VidCoder đã được mở!", "Green");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi cài đặt VidCoder: {ex.Message}", "Red");
            }
        }

        /// <summary>
        /// Tìm phiên bản VidCoder mới nhất từ GitHub Releases
        /// </summary>
        private async Task<string> GetLatestVidCoderVersionAsync()
        {
            try
            {
                // Sử dụng GitHub API để lấy danh sách releases
                string apiUrl = "https://api.github.com/repos/RandomEngy/VidCoder/releases";
                
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
                request.UserAgent = "GMTPC-Tool";
                request.Accept = "application/json";

                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    string json = await reader.ReadToEndAsync();
                    
                    // Parse JSON đơn giản để tìm tất cả versions
                    var versions = new List<(string Version, int BuildNumber)>();
                    
                    // Tìm tất cả các tag_name có dạng v*
                    int startIndex = 0;
                    while ((startIndex = json.IndexOf("\"tag_name\":", startIndex)) != -1)
                    {
                        startIndex += "\"tag_name\":".Length;
                        int quoteStart = json.IndexOf('"', startIndex);
                        if (quoteStart == -1) break;
                        
                        quoteStart++;
                        int quoteEnd = json.IndexOf('"', quoteStart);
                        if (quoteEnd == -1) break;
                        
                        string tagName = json.Substring(quoteStart, quoteEnd - quoteStart);
                        
                        // Chỉ lấy các tag có dạng vX.Y.Z
                        if (tagName.StartsWith("v") && tagName.Length > 1)
                        {
                            // Parse version number để so sánh
                            string versionNum = tagName.TrimStart('v');
                            int buildNumber = ParseVersionToNumber(versionNum);
                            versions.Add((tagName, buildNumber));
                        }
                        
                        startIndex = quoteEnd + 1;
                    }

                    // Tìm version có số build lớn nhất
                    if (versions.Count > 0)
                    {
                        var latest = versions.OrderByDescending(v => v.BuildNumber).First();
                        return latest.Version;
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi khi tìm phiên bản VidCoder: {ex.Message}", "Orange");
            }

            return null;
        }

        /// <summary>
        /// Chuyển version string (X.Y.Z) thành số để so sánh
        /// </summary>
        private int ParseVersionToNumber(string version)
        {
            try
            {
                var parts = version.Split('.');
                if (parts.Length >= 3)
                {
                    int major = int.TryParse(parts[0], out var m) ? m : 0;
                    int minor = int.TryParse(parts[1], out var n) ? n : 0;
                    int build = int.TryParse(parts[2], out var b) ? b : 0;
                    
                    // Công thức: major * 1000000 + minor * 1000 + build
                    return major * 1000000 + minor * 1000 + build;
                }
            }
            catch { }

            return 0;
        }
    }
}
