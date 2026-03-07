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

        private void ChkInstallIDM_Click(object sender, RoutedEventArgs e)
        {
            if (ChkInstallIDM.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Internet Download Manager", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Internet Download Manager", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkInstallWinRAR_Click(object sender, RoutedEventArgs e)
        {
            if (ChkInstallWinRAR.IsChecked == true)
            {
                UpdateStatus("Đã chọn: WinRAR", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: WinRAR", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkInstallBID_Click(object sender, RoutedEventArgs e)
        {
            if (ChkInstallBID.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Bulk Image Downloader", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Bulk Image Downloader", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkActivateWindows_Click(object sender, RoutedEventArgs e)
        {
            if (ChkActivateWindows.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Tự động kích hoạt Windows", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Tự động kích hoạt Windows", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkPauseWindowsUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (ChkPauseWindowsUpdate.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Pause Windows Update", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Pause Windows Update", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkVcredist_Click(object sender, RoutedEventArgs e)
        {
            if (ChkVcredist.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Vcredist 2005-2022", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Vcredist 2005-2022", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkDirectX_Click(object sender, RoutedEventArgs e)
        {
            if (ChkDirectX.IsChecked == true)
            {
                UpdateStatus("Đã chọn: DirectX", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: DirectX", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkJava_Click(object sender, RoutedEventArgs e)
        {
            if (ChkJava.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Java", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Java", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkOpenAL_Click(object sender, RoutedEventArgs e)
        {
            if (ChkOpenAL.IsChecked == true)
            {
                UpdateStatus("Đã chọn: OpenAL", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: OpenAL", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void Chk3DPChip_Click(object sender, RoutedEventArgs e)
        {
            if (Chk3DPChip.IsChecked == true)
            {
                UpdateStatus("Đã chọn: 3DP Chip", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: 3DP Chip", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void Chk3DPNet_Click(object sender, RoutedEventArgs e)
        {
            if (Chk3DPNet.IsChecked == true)
            {
                UpdateStatus("Đã chọn: 3DP Net", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: 3DP Net", "Yellow");
            }

            UpdateInstallButtonState();
        }


        private void ChkRevoUninstaller_Click(object sender, RoutedEventArgs e)
        {
            if (ChkRevoUninstaller.IsChecked == true)
            {
                UpdateStatus("Đã chọn: Revo Uninstaller", "Green");
            }
            else
            {
                UpdateStatus("Đã hủy chọn: Revo Uninstaller", "Yellow");
            }

            UpdateInstallButtonState();
        }

    }
}
