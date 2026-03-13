using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
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
                // Scrape/Resolve MediaFire page để lấy direct link hỗ trợ Range
                string downloadUrl = await ScrapeMediaFireUrlAsync(mediafirePageUrl);

                if (string.IsNullOrEmpty(downloadUrl))
                    throw new Exception("Không tìm được link download từ MediaFire. Vui lòng thử lại.");

                UpdateStatus("Đã lấy link, đang tải về ổ C (16 segments)...", "Cyan");

                // Dùng engine 16-segment — MediaFire hỗ trợ Range requests nên sẽ tận dụng được đa luồng
                await DownloadWithProgressAsync(downloadUrl, destinationPath, "Đang tải về ổ C - Win 10 LTSC IoT 21H2 - mediafire");

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
        /// Lấy direct download link từ trang MediaFire bằng 3 chiến lược:
        ///
        ///   Chiến lược 1 — MediaFire Public API (đáng tin cậy nhất):
        ///     Trích quick_key từ URL, gọi /api/1.4/file/get_links.php?quick_key=KEY&type=skip_ads
        ///     để nhận JSON với direct link — không bị JS block.
        ///
        ///   Chiến lược 2 — Scrape HTML (fallback nếu API thất bại):
        ///     Tìm thẻ <a> có class "downloadButton" hoặc aria-label="Download file".
        ///
        ///   Chiến lược 3 — Regex (fallback cuối cùng):
        ///     Tìm URL download*.mediafire.com trong HTML/script.
        /// </summary>
        private async Task<string> ScrapeMediaFireUrlAsync(string pageUrl)
        {
            // ── Chiến lược 1: MediaFire Public API ──────────────────────────────────
            // Trích quick_key từ URL (segment giữa /file/ và /)
            // Ví dụ: mediafire.com/file/b54r3aup07dddhe/filename/file → key = b54r3aup07dddhe
            var keyMatch = Regex.Match(pageUrl,
                @"mediafire\.com/file/([a-z0-9]+)/",
                RegexOptions.IgnoreCase);

            if (keyMatch.Success)
            {
                string quickKey = keyMatch.Groups[1].Value;
                string apiUrl = $"https://www.mediafire.com/api/1.4/file/get_links.php?quick_key={quickKey}&type=skip_ads&response_format=json";

                try
                {
                    using (var apiClient = new HttpClient())
                    {
                        apiClient.Timeout = TimeSpan.FromSeconds(20);
                        apiClient.DefaultRequestHeaders.Add("User-Agent",
                            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                        string json = await apiClient.GetStringAsync(apiUrl);

                        // Parse JSON bằng Regex (tương thích C# 7.3/.NET 4.8, không cần assembly bổ sung)
                        // JSON dạng: {"response":{"links":[{"normal_download":"https://..."}]}}
                        var mNormal = Regex.Match(json, @"""normal_download""\s*:\s*""(https://[^""]+)""", RegexOptions.IgnoreCase);
                        if (mNormal.Success && !string.IsNullOrEmpty(mNormal.Groups[1].Value))
                            return mNormal.Groups[1].Value;

                        var mDirect = Regex.Match(json, @"""direct_download""\s*:\s*""(https://[^""]+)""", RegexOptions.IgnoreCase);
                        if (mDirect.Success && !string.IsNullOrEmpty(mDirect.Groups[1].Value))
                            return mDirect.Groups[1].Value;
                    }
                }
                catch { /* API thất bại → thử scrape HTML */ }
            }

            // ── Chiến lược 2 & 3: Scrape HTML ───────────────────────────────────────
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept",
                    "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8");
                client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");
                client.Timeout = TimeSpan.FromSeconds(30);

                string html = await client.GetStringAsync(pageUrl);

                // Chiến lược 2: XPath scrape
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                var dlNode = doc.DocumentNode.SelectSingleNode(
                    "//a[contains(@class,'downloadButton')] | //a[@aria-label='Download file'] | //a[@id='downloadButton'] | //a[contains(@class,'download')]");

                if (dlNode != null)
                {
                    string href = dlNode.GetAttributeValue("href", null);
                    if (!string.IsNullOrEmpty(href) && href.StartsWith("http"))
                        return href;
                }

                // Chiến lược 3: Regex tìm download*.mediafire.com trong toàn bộ HTML (kể cả script)
                var m1 = Regex.Match(html,
                    @"""(https://download\d*\.mediafire\.com/[^""]+)""",
                    RegexOptions.IgnoreCase);
                if (m1.Success) return m1.Groups[1].Value;

                // Regex mở rộng hơn — tìm bất kỳ URL mediafire CDN
                var m2 = Regex.Match(html,
                    @"(https://[a-z0-9]+\.mediafire\.com/download/[^""'\s]+)",
                    RegexOptions.IgnoreCase);
                if (m2.Success) return m2.Groups[1].Value;

                return null;
            }
        }
    }
}
