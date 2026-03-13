// AI CONTEXT:
// App.xaml.cs – Application lifecycle and startup checks.
// Ensures the app runs with Administrator privileges before launching UI.
// TCP & ThreadPool optimizations for peak download speed.
using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Windows;

namespace GMTPC.Tool
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // ================================================================
            // TCP & ThreadPool Optimization - Achieve Peak Speed Immediately
            // ================================================================
            
            // Force .NET to allocate minimum threads upfront (eliminates Thread Injection Rate delay)
            // 100 worker threads + 100 completion port threads for 16 segments per download
            ThreadPool.SetMinThreads(100, 100);
            
            // Disable default .NET network delays for instant TCP connection
            ServicePointManager.UseNagleAlgorithm = false;           // Disable Nagle (reduces latency)
            ServicePointManager.Expect100Continue = false;           // Disable 100-continue (eliminates round-trip)
            ServicePointManager.DefaultConnectionLimit = 100;        // Allow 100 concurrent connections per host
            ServicePointManager.MaxServicePoints = 100;              // Cache up to 100 ServicePoints
            ServicePointManager.SetTcpKeepAlive(true, 10000, 1000);  // TCP keepalive: 10s idle, 1s interval
            
            // ================================================================
            
            // Kiểm tra quyền Administrator
            if (!IsRunningAsAdministrator())
            {
                // Thử khởi chạy lại chính ứng dụng với quyền admin (sẽ hiển thị UAC prompt)
                try
                {
                    string exePath = Assembly.GetEntryAssembly()?.Location ?? Process.GetCurrentProcess().MainModule.FileName;
                    string args = Environment.CommandLine;

                    // Nếu Environment.CommandLine trả về cả đường dẫn, bỏ phần đầu (tên exe) để chỉ giữ args
                    // Sử dụng e.Args là an toàn hơn
                    string arguments = string.Empty;
                    if (e != null && e.Args != null && e.Args.Length > 0)
                    {
                        arguments = string.Join(" ", e.Args);
                    }

                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        Arguments = arguments,
                        UseShellExecute = true,
                        Verb = "runas"
                    };

                    Process.Start(psi);
                    // Khởi chạy mới đã được yêu cầu - đóng process hiện tại
                    Environment.Exit(0);
                    return;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Người dùng hủy UAC hoặc không thể khởi chạy với admin
                    MessageBox.Show("Ứng dụng cần quyền Administrator để chạy. Hành động bị hủy.", "Quyền Administrator cần thiết", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Environment.Exit(0);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi cố gắng chạy với quyền admin: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    Environment.Exit(0);
                    return;
                }
            }

            base.OnStartup(e);
        }

        private bool IsRunningAsAdministrator()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}