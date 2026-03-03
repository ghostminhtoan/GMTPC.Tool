using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using HtmlAgilityPack;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        private void ChkWin10LtscIot21H2_Click(object sender, RoutedEventArgs e)
        {
            UpdateInstallButtonState();
        }

        private async Task InstallWin10LtscIot21H2Async()
        {
            string mediafirePageUrl = "https://www.mediafire.com/file/b54r3aup07dddhe/win_10_LTSC_IOT_21H2_-_2021%E2%88%9510_-_No_Defender_Office_-_MMTPC_3.0.iso/file";
            string fileName = "win_10_LTSC_IOT_21H2_MMTPC_3.0.iso";
            string destinationPath = System.IO.Path.Combine("C:\\", fileName);

            UpdateStatus("Đang lấy link download từ MediaFire...", "Cyan");

            try
            {
                // Scrape MediaFire page để lấy link download thực
                string downloadUrl = await ScrapeMediaFireUrlAsync(mediafirePageUrl);

                if (string.IsNullOrEmpty(downloadUrl))
                    throw new Exception("Không tìm được link download từ MediaFire. Vui lòng thử lại.");

                UpdateStatus("Đang tải về ổ C", "Cyan");
                // Tải với retry logic để xử lý connection stalled
                await DownloadWithRetryAsync(downloadUrl, destinationPath, "Đang tải về ổ C - Win 10 LTSC IoT 21H2 - mediafire", maxRetries: 5);

                UpdateStatus("Tải xong! Đang mở ổ C và file ISO...", "Green");
                Process.Start("explorer.exe", $"/select,{destinationPath}");
                Process.Start(new ProcessStartInfo { FileName = destinationPath, UseShellExecute = true });
            }
            catch (OperationCanceledException)
            {
                UpdateStatus("Đã hủy tải Win 10 LTSC IoT 21H2.", "Yellow");
                if (File.Exists(destinationPath))
                    try { File.Delete(destinationPath); } catch { }
                throw;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Lỗi tải Win 10 LTSC IoT 21H2: {ex.Message}", "Red");
                throw;
            }
        }

        /// <summary>
        /// Scrape trang MediaFire để tìm link download thực sự.
        /// Tìm thẻ <a> có class="downloadButton" hoặc aria-label="Download file".
        /// </summary>
        private async Task<string> ScrapeMediaFireUrlAsync(string pageUrl)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.Timeout = TimeSpan.FromSeconds(30);

                string html = await client.GetStringAsync(pageUrl);

                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Cách 1: Tìm thẻ <a> có class chứa "downloadButton"
                var dlNode = doc.DocumentNode.SelectSingleNode(
                    "//a[contains(@class,'downloadButton')] | //a[@aria-label='Download file'] | //a[@id='downloadButton']");

                if (dlNode != null)
                {
                    string href = dlNode.GetAttributeValue("href", null);
                    if (!string.IsNullOrEmpty(href)) return href;
                }

                // Cách 2: Dùng regex để tìm link download trực tiếp trong script tag
                var match = System.Text.RegularExpressions.Regex.Match(
                    html,
                    @"""(https://download\d+\.mediafire\.com/[^""]+)""",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success) return match.Groups[1].Value;

                return null;
            }
        }
    }
}
