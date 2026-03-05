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
        private void PopulateSystemInfo()
        {
            try
            {
                // Mainboard
                string mainboard = GetWmiSingleValue("Win32_BaseBoard", "Product") ?? GetWmiSingleValue("Win32_BaseBoard", "Manufacturer") ?? "Unknown";
                TbMainboard.Text = mainboard;

                // CPU
                string cpuName = GetWmiSingleValue("Win32_Processor", "Name") ?? "Unknown";
                string cpuClock = GetWmiSingleValue("Win32_Processor", "MaxClockSpeed");
                string cores = GetWmiSingleValue("Win32_Processor", "NumberOfCores");
                string threads = GetWmiSingleValue("Win32_Processor", "NumberOfLogicalProcessors");
                string cpuInfo = cpuName;
                if (!string.IsNullOrEmpty(cpuClock)) cpuInfo += $" - {cpuClock} MHz";
                cpuInfo += $" ({cores} cores / {threads} threads)";
                TbCPU.Text = cpuInfo;

                // RAM
                ulong totalRamBytes = 0;
                try
                {
                    var search = new ManagementObjectSearcher("select Capacity from Win32_PhysicalMemory");
                    foreach (ManagementObject mo in search.Get())
                    {
                        if (mo["Capacity"] != null)
                        {
                            ulong part = Convert.ToUInt64(mo["Capacity"]);
                            totalRamBytes += part;
                        }
                    }
                }
                catch { }
                TbRAM.Text = totalRamBytes > 0 ? FormatBytes(totalRamBytes) : "Unknown";

                // GPU
                string gpu = GetWmiSingleValue("Win32_VideoController", "Name") ?? "Unknown";
                TbGPU.Text = gpu;

                // Windows product and build
                try
                {
                    using (var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion"))
                    {
                        if (key != null)
                        {
                            string productName = key.GetValue("ProductName") as string ?? "Windows";
                            string build = key.GetValue("CurrentBuild")?.ToString() ?? key.GetValue("CurrentBuildNumber")?.ToString() ?? "";
                            string ubr = key.GetValue("UBR")?.ToString();
                            string edition = key.GetValue("EditionID") as string ?? "";
                            string winText = productName;
                            if (!string.IsNullOrEmpty(edition)) winText += $" {edition}";
                            if (!string.IsNullOrEmpty(build)) winText += $" (Build {build}{(ubr != null ? "." + ubr : "")})";
                            TbWindows.Text = winText;
                        }
                    }
                }
                catch { TbWindows.Text = "Unknown"; }

                // DirectX version
                try
                {
                    TbDirectX.Text = GetDirectXVersion();
                }
                catch { TbDirectX.Text = "Unknown"; }
            }
            catch (Exception ex)
            {
                // don't crash UI
                try { TbMainboard.Text = "Error: " + ex.Message; } catch { }
            }
        }

        private string GetDirectXVersion()
        {
            try
            {
                // Most modern Windows (10/11) strictly use DirectX 12
                // We can also check the registry for a broad version string
                string versionStr = "";
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\DirectX"))
                {
                    versionStr = key?.GetValue("Version") as string ?? "";
                }

                // Map common internal version strings to friendly names
                if (versionStr.StartsWith("4.09.00.0904"))
                {
                    // This is the common string for DirectX 9.0c, but also often remains on Win10/11
                    // Detection by OS version is more reliable for modern DX
                    if (IsWindows10Or11()) return "DirectX 12";
                    return "DirectX 9.0c";
                }

                // Detailed mapping if needed
                switch (versionStr)
                {
                    case "4.09.00.0904": return "DirectX 9.0c";
                    case "4.09.00.0902": return "DirectX 9.0b";
                    case "4.09.00.0900": return "DirectX 9.0";
                    case "4.08.01.0881": return "DirectX 8.1";
                    case "4.08.00.0400": return "DirectX 8.0";
                    case "4.07.00.0700": return "DirectX 7.0";
                }

                if (IsWindows10Or11()) return "DirectX 12";
                if (IsWindows8Or81()) return "DirectX 11.1/11.2";
                if (IsWindows7()) return "DirectX 11";

                return !string.IsNullOrEmpty(versionStr) ? $"DirectX {versionStr}" : "Unknown";
            }
            catch { return "Unknown"; }
        }

        private bool IsWindows10Or11()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    string buildStr = key?.GetValue("CurrentBuild")?.ToString() ?? "";
                    if (int.TryParse(buildStr, out int build))
                    {
                        return build >= 10240; // Windows 10 build 10240 is the first release
                    }
                }
            }
            catch { }
            return Environment.OSVersion.Version.Major >= 10;
        }

        private bool IsWindows8Or81()
        {
            return Environment.OSVersion.Version.Major == 6 && (Environment.OSVersion.Version.Minor == 2 || Environment.OSVersion.Version.Minor == 3);
        }

        private bool IsWindows7()
        {
            return Environment.OSVersion.Version.Major == 6 && Environment.OSVersion.Version.Minor == 1;
        }

        private string GetWmiSingleValue(string wmiClass, string property)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"select {property} from {wmiClass}");
                foreach (ManagementObject mo in searcher.Get())
                {
                    if (mo[property] != null)
                    {
                        return mo[property].ToString();
                    }
                }
            }
            catch { }
            return null;
        }

        private string FormatBytes(ulong bytes)
        {
            double gb = bytes / (1024.0 * 1024.0 * 1024.0);
            return $"{gb:F2} GB";
        }
    }
}
