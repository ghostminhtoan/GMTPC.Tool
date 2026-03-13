// =======================================================================
// MainWindow.TabDriver.cs
// Chức năng: Xử lý checkbox và cài đặt cho Tab Driver
// Cập nhật: 2026-03-10 - Tạo file mới cho Tab Driver với 3DP Chip và 3DP Net
// =======================================================================
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // ===================================================================
        // TabDriver — Checkbox Click Handlers
        // TabItem Header: "Driver"
        // Checkboxes: Chk3DPChip, Chk3DPNet
        // ===================================================================
        private void Chk3DPChip_Click(object sender, RoutedEventArgs e)
        {
            if (Chk3DPChip.IsChecked == true)
            {
                UpdateStatus("Đã chọn: 3DP Chip (all driver trừ internet)", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: 3DP Chip (all driver trừ internet)", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void Chk3DPNet_Click(object sender, RoutedEventArgs e)
        {
            if (Chk3DPNet.IsChecked == true)
            {
                UpdateStatus("Đã chọn: 3DP Net - driver internet", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: 3DP Net - driver internet", "Yellow");
            }

            UpdateInstallButtonState();
        }

        // ===================================================================
        // TabDriver — Install Methods
        // ===================================================================
        private Task Run3DPChipAsync()
        {
            Btn3DPChip_Click(null, null);
            return Task.CompletedTask;
        }

        private Task Install3DPNetAsync()
        {
            Btn3DPNet_Click(null, null);
            return Task.CompletedTask;
        }

        // ===================================================================
        // TabDriver — Button Click Handlers (actual implementation)
        // ===================================================================
        private async void Btn3DPChip_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang chạy 3DP Chip - all driver trừ internet...", "Cyan");
            string driverChipPath = Path.Combine(@"R:\HDD R\ZC SYMLINK\USERS\Downloads\Programs", "3DP_Chip_v2510.exe");
            if (File.Exists(driverChipPath))
            {
                try
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo { FileName = driverChipPath, UseShellExecute = true };
                    Process process = Process.Start(startInfo);
                    if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus(process.ExitCode == 0 ? "3DP Chip hoàn tất!" : $"Mã lỗi: {process.ExitCode}", process.ExitCode == 0 ? "Green" : "Red"); }
                }
                catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
            }
            else { UpdateStatus("Không tìm thấy file 3DP_Chip_v2510.exe", "Red"); }
        }

        private async void Btn3DPNet_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatus("Đang tải 3DP Net - driver internet...", "Cyan");
            string driverNetPath = Path.Combine(GetGMTPCFolder(), "3DP_Net_v2101.exe");
            try
            {
                await DownloadWithProgressAsync("https://github.com/ghostminhtoan/MMT/releases/download/v1.0/3DP.Net.exe", driverNetPath, "3DP Net Driver Installer");
                UpdateStatus("Đang chạy 3DP Net với lệnh /y...", "Yellow");
                ProcessStartInfo startInfo = new ProcessStartInfo { FileName = driverNetPath, Arguments = "/y", UseShellExecute = true };
                Process process = Process.Start(startInfo);
                if (process != null) { await Task.Run(() => process.WaitForExit()); UpdateStatus(process.ExitCode == 0 ? "3DP Net hoàn tất!" : $"Mã lỗi: {process.ExitCode}", process.ExitCode == 0 ? "Green" : "Red"); }
                if (File.Exists(driverNetPath)) File.Delete(driverNetPath);
            }
            catch (Exception ex) { UpdateStatus($"Lỗi: {ex.Message}", "Red"); }
        }
    }
}
