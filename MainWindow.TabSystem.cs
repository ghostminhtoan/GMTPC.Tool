// =======================================================================
// MainWindow.TabSystem.cs
// Chức năng: Xử lý checkbox hệ thống (PowerISO, TeraCopy, VPN1111, Google Drive, etc.)
// Cập nhật: 2026-03-10 - Tạo file mới cho Tab System
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

/*
 * AI Summary:
 * Date: 2026-03-09
 * - Added ChkTeraCopy and ChkVPN1111 references
 */

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        private void ChkTeraCopy_Click(object sender, RoutedEventArgs e)
        {
            if (ChkTeraCopy.IsChecked == true)
            {
                UpdateStatus("Đã chọn: TeraCopy", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: TeraCopy", "Yellow");
            }

            UpdateInstallButtonState();
        }

        private void ChkVPN1111_Click(object sender, RoutedEventArgs e)
        {
            if (ChkVPN1111.IsChecked == true)
            {
                UpdateStatus("Đã chọn: VPN 1111 (Cloudflare)", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: VPN 1111 (Cloudflare)", "Yellow");
            }

            UpdateInstallButtonState();
        }
    }
}
