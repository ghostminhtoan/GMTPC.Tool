// =======================================================================
// MainWindow.TabPartition.cs
// Chức năng: Xử lý checkbox và công cụ partition (DiskGenius, AOMEI)
// Cập nhật: 2026-03-10 - Tạo file mới cho Tab Partition
// Cập nhật: 2026-03-14 - Updated to use DownloadSingleLinkFastAsync (16 segments)
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

        // Event handler stubs for missing CheckBoxes
        private void ChkDiskGenius_Click(object sender, RoutedEventArgs e)
        {
            if (ChkDiskGenius.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Disk Genius", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Disk Genius", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkAomeiPartitionAssistant_Click(object sender, RoutedEventArgs e)
        {
            if (ChkAomeiPartitionAssistant.IsChecked == true)
            {
                UpdateStatus("Đã chọn: AOMEI Partition Assistant", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: AOMEI Partition Assistant", "Yellow");
            }

            UpdateInstallButtonState();
        }


        // InstallDiskGeniusAsync() -> Moved to MainWindow.SystemArguments.cs
        // (có MessageBox.Show + /s argument)

        // InstallAomeiPartitionAssistantAsync() -> Moved to MainWindow.SystemArguments.cs
        // (có MessageBox.Show + /passive argument)

    }
}
