// =======================================================================
// MainWindow.DriveSelector.cs
// Chức năng: Drive ComboBox, auto path routing, multi-task queue
// Cập nhật: 2026-03-15 - Merged DriveScannerService from Services folder
// Previous: 2026-03-12 - Refactored for concurrency & state management
// =======================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // ===================================================================
        // DriveInfoEx Class - Represents information about a detected drive
        // ===================================================================
        public class DriveInfoEx
        {
            public string DriveLetter { get; set; }
            public string DriveName { get; set; }
            public string DriveType { get; set; }
            public long FreeSpaceBytes { get; set; }
            public long TotalSizeBytes { get; set; }
            public string VolumeLabel { get; set; }
            public string DriveFormat { get; set; }
            public bool IsReady { get; set; }

            /// <summary>
            /// Display format: [Drive Letter/Name] - [Free Space]
            /// Example: "D:/Samsung 980 PRO - 512.3 GB free"
            /// </summary>
            public string DisplayName
            {
                get
                {
                    var freeSpace = FormatBytesUtility(FreeSpaceBytes);
                    var namePart = string.IsNullOrWhiteSpace(DriveName) ? DriveLetter : DriveName;
                    return $"{DriveLetter} {namePart} - {freeSpace} free";
                }
            }

            /// <summary>
            /// Short display format for compact UI: [Drive Letter] - [Free Space]
            /// Example: "D: - 512 GB"
            /// </summary>
            public string ShortDisplayName
            {
                get
                {
                    var freeSpace = FormatBytesUtility(FreeSpaceBytes);
                    return $"{DriveLetter} - {freeSpace}";
                }
            }

            /// <summary>
            /// Gets the temp folder path for this drive (e.g., "K:\temp")
            /// Format: {DriveLetter}:\temp
            /// </summary>
            public string TempFolderPath => Path.Combine(DriveLetter + ":", "temp");

            /// <summary>
            /// Utility method to format bytes to human-readable format
            /// </summary>
            private static string FormatBytesUtility(long bytes)
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double size = bytes;

                while (size >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    size /= 1024;
                }

                return $"{size:0.##} {sizes[order]}";
            }
        }

        // Drive scanner fields
        private List<DriveInfoEx> _detectedDrives;
        private bool _isScanningDrives;

        // ===================================================================
        // DriveScannerService - Static methods for scanning drives
        // ===================================================================
        // Drive types to exclude (CD/DVD drives)
        private static readonly DriveType[] ExcludedDriveTypes = new[]
        {
            DriveType.CDRom,
            DriveType.Unknown,
            DriveType.NoRootDirectory
        };

        /// <summary>
        /// Scans all drives asynchronously without freezing UI
        /// Optimized for systems with multiple drive types
        /// </summary>
        /// <param name="includeNetworkDrives">Include network drives (slower)</param>
        /// <returns>List of detected drives with info</returns>
        public static Task<List<DriveInfoEx>> ScanDrivesAsync(bool includeNetworkDrives = false)
        {
            return Task.Run(() =>
            {
                var drives = new List<DriveInfoEx>();

                try
                {
                    // Get all drives - this is fast for local drives
                    var allDrives = DriveInfo.GetDrives();

                    foreach (var drive in allDrives)
                    {
                        try
                        {
                            // Skip excluded drive types (CD/DVD)
                            if (ExcludedDriveTypes.Contains(drive.DriveType))
                            {
                                continue;
                            }

                            // Skip network drives unless explicitly requested
                            if (!includeNetworkDrives && drive.DriveType == DriveType.Network)
                            {
                                continue;
                            }

                            var driveInfo = new DriveInfoEx
                            {
                                DriveLetter = drive.Name.TrimEnd('\\'),
                                DriveType = drive.DriveType.ToString(),
                                IsReady = drive.IsReady
                            };

                            // Only query detailed info if drive is ready
                            // This prevents hanging on removable drives without media
                            if (drive.IsReady)
                            {
                                try
                                {
                                    driveInfo.FreeSpaceBytes = drive.AvailableFreeSpace;
                                    driveInfo.TotalSizeBytes = drive.TotalSize;
                                    driveInfo.DriveFormat = drive.DriveFormat;

                                    // Try to get volume label
                                    try
                                    {
                                        driveInfo.VolumeLabel = drive.VolumeLabel;
                                    }
                                    catch
                                    {
                                        // Some drives don't allow volume label access
                                        driveInfo.VolumeLabel = string.Empty;
                                    }

                                    // Get friendly drive name using WMI (for SSD/NVMe detection)
                                    driveInfo.DriveName = GetFriendlyDriveName(drive.Name.TrimEnd('\\'))
                                        ?? driveInfo.VolumeLabel
                                        ?? $"{drive.DriveType} Drive";
                                }
                                catch (IOException)
                                {
                                    // Drive became unavailable during query
                                    driveInfo.IsReady = false;
                                }
                                catch (UnauthorizedAccessException)
                                {
                                    // Insufficient permissions
                                    driveInfo.IsReady = false;
                                }
                            }
                            else
                            {
                                driveInfo.DriveName = $"{drive.DriveType} (Not Ready)";
                            }

                            drives.Add(driveInfo);
                        }
                        catch (Exception ex)
                        {
                            // Log individual drive errors but continue scanning
                            Debug.WriteLine($"Error scanning drive {drive.Name}: {ex.Message}");
                        }
                    }

                    // Sort by drive letter for consistent display
                    return drives.OrderBy(d => d.DriveLetter).ToList();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error scanning drives: {ex.Message}");
                    return new List<DriveInfoEx>();
                }
            });
        }

        /// <summary>
        /// Gets a friendly name for a drive using WMI
        /// Returns model name for SSDs/NVMe drives
        /// </summary>
        private static string GetFriendlyDriveName(string driveLetter)
        {
            try
            {
                // Extract just the letter (e.g., "C" from "C:\")
                var letter = driveLetter.TrimEnd(':', '\\');

                // Query disk drives via WMI
                var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject drive in searcher.Get())
                {
                    try
                    {
                        var deviceId = drive["DeviceID"]?.ToString() ?? "";

                        // Check if this drive contains our letter using association queries
                        var partitionQuery = new ManagementObjectSearcher(
                            $"ASSOCIATORS OF {{Win32_DiskDrive.DeviceID='{deviceId}'}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");

                        foreach (ManagementObject partition in partitionQuery.Get())
                        {
                            var logicalDiskQuery = new ManagementObjectSearcher(
                                $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID='{partition["DeviceID"]}'}} WHERE AssocClass=Win32_LogicalDiskToDiskPartition");

                            foreach (ManagementObject logicalDisk in logicalDiskQuery.Get())
                            {
                                var logicalDeviceId = logicalDisk["DeviceID"]?.ToString() ?? "";
                                if (string.Equals(logicalDeviceId, letter, StringComparison.OrdinalIgnoreCase))
                                {
                                    // Found matching drive, return model name
                                    var model = drive["Model"]?.ToString() ?? "";
                                    if (!string.IsNullOrWhiteSpace(model))
                                    {
                                        // Clean up model name (remove extra spaces)
                                        return System.Text.RegularExpressions.Regex.Replace(model, @"\s+", " ").Trim();
                                    }
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Ignore individual drive errors
                    }
                }
            }
            catch
            {
                // WMI query failed, return null
            }

            return null;
        }

        /// <summary>
        /// Ensures a temp folder exists on the specified drive
        /// Creates it if necessary with proper exception handling
        /// Hardcoded pattern: {DriveLetter}:\temp (e.g., K:\temp)
        /// </summary>
        /// <param name="driveLetter">Drive letter (e.g., "K")</param>
        /// <returns>Full path to temp folder, or null if creation failed</returns>
        public static string EnsureTempFolder(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
            {
                throw new ArgumentNullException(nameof(driveLetter));
            }

            // Extract just the letter if needed
            var letter = driveLetter.TrimEnd(':', '\\');
            // HARDCODED PATTERN: {DriveLetter}:\temp - NOT using Path.GetTempPath()
            var tempPath = Path.Combine(letter + ":", "temp");

            try
            {
                // Ensure directory exists before returning
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }
                return tempPath;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.WriteLine($"Unauthorized access creating temp folder: {ex.Message}");
                throw new UnauthorizedAccessException(
                    $"Insufficient permissions to create temp folder at {tempPath}. " +
                    $"Please run as administrator or choose a different drive.");
            }
            catch (IOException ex)
            {
                Debug.WriteLine($"IO error creating temp folder: {ex.Message}");
                throw new IOException(
                    $"Failed to create temp folder at {tempPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates if a drive is suitable for temporary downloads
        /// </summary>
        /// <param name="driveLetter">Drive letter to validate</param>
        /// <param name="minimumFreeSpaceBytes">Minimum required free space</param>
        /// <returns>True if drive is suitable</returns>
        public static bool ValidateDriveForDownload(string driveLetter, long minimumFreeSpaceBytes = 0)
        {
            try
            {
                var letter = driveLetter.TrimEnd(':', '\\');
                var drive = new DriveInfo(letter + ":");

                if (!drive.IsReady)
                {
                    return false;
                }

                if (ExcludedDriveTypes.Contains(drive.DriveType))
                {
                    return false;
                }

                if (minimumFreeSpaceBytes > 0 && drive.AvailableFreeSpace < minimumFreeSpaceBytes)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Initialize drive scanner on window load
        /// </summary>
        private async void InitializeDriveScanner()
        {
            if (_isScanningDrives) return;
            
            _isScanningDrives = true;
            try
            {
                await ScanAndPopulateDrivesAsync();
            }
            finally
            {
                _isScanningDrives = false;
            }
        }

        /// <summary>
        /// Scan drives and populate the ComboBox asynchronously
        /// </summary>
        private async Task ScanAndPopulateDrivesAsync()
        {
            try
            {
                UpdateSecondaryStatus("Scanning drives...", "Cyan");

                // Scan drives on background thread
                _detectedDrives = await ScanDrivesAsync();
                
                // Update UI on dispatcher thread
                Dispatcher.Invoke(() =>
                {
                    CboDriveSelector.Items.Clear();

                    if (_detectedDrives.Count == 0)
                    {
                        var placeholderDrive = new DriveInfoEx
                        {
                            DriveLetter = "No drives found",
                            DriveName = "No drives detected"
                        };
                        CboDriveSelector.Items.Add(placeholderDrive);
                        CboDriveSelector.IsEnabled = false;
                    }
                    else
                    {
                        // Add all drives to ComboBox
                        foreach (var drive in _detectedDrives)
                        {
                            CboDriveSelector.Items.Add(drive);
                        }

                        // Select system drive by default (usually C:)
                        var systemDrive = _detectedDrives.FirstOrDefault(d =>
                            d.DriveLetter.StartsWith("C:", StringComparison.OrdinalIgnoreCase));

                        if (systemDrive != null)
                        {
                            CboDriveSelector.SelectedItem = systemDrive;
                        }
                        else
                        {
                            CboDriveSelector.SelectedIndex = 0;
                        }

                        CboDriveSelector.IsEnabled = true;
                    }

                    UpdateSecondaryStatus("Ready", "Green");
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    UpdateSecondaryStatus($"Drive scan error: {ex.Message}", "Red");
                    CboDriveSelector.Items.Clear();
                    var errorDrive = new DriveInfoEx
                    {
                        DriveLetter = "Error",
                        DriveName = "Error scanning drives"
                    };
                    CboDriveSelector.Items.Add(errorDrive);
                    CboDriveSelector.IsEnabled = false;
                });
            }
        }

        /// <summary>
        /// Handle drive selection change - Auto path routing
        /// Sets global temp base path: {DriveLetter}:\temp
        /// </summary>
        private void CboDriveSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CboDriveSelector.SelectedItem is DriveInfoEx selectedDrive)
            {
                try
                {
                    // Set global temp base path using DownloadConfiguration
                    string tempBasePath = DownloadConfiguration.SetTempBasePath(selectedDrive.DriveLetter);

                    UpdateSecondaryStatus($"Temp: {tempBasePath}", "Cyan");
                    UpdateStatus($"Global temp set: {selectedDrive.DriveLetter}:\temp", "Green");
                }
                catch (UnauthorizedAccessException ex)
                {
                    UpdateStatus($"Permission denied: {ex.Message}", "Red");
                    DownloadConfiguration.TempBasePath = null;
                }
                catch (IOException ex)
                {
                    UpdateStatus($"IO Error: {ex.Message}", "Red");
                    DownloadConfiguration.TempBasePath = null;
                }
                catch (Exception ex)
                {
                    UpdateStatus($"Error: {ex.Message}", "Red");
                    DownloadConfiguration.TempBasePath = null;
                }
            }
        }

        /// <summary>
        /// Refresh drive list button click
        /// </summary>
        private async void BtnRefreshDrives_Click(object sender, RoutedEventArgs e)
        {
            await ScanAndPopulateDrivesAsync();
            UpdateStatus("Drive list refreshed", "Green");
        }

        /// <summary>
        /// Get the currently selected temp path
        /// Returns global temp base path from DownloadConfiguration
        /// Format: {DriveLetter}:\temp
        /// </summary>
        private string GetSelectedTempPath()
        {
            return DownloadConfiguration.TempBasePath ?? GetGMTPCFolder();
        }

        // ===================================================================
        // Multi-Task Queue Implementation
        // ===================================================================

        /// <summary>
        /// Collects all selected tasks from checkboxes across all tabs
        /// </summary>
        private List<(Func<Task> Action, CheckBox CheckBox, string Name)> CollectSelectedTasks()
        {
            var tasks = new List<(Func<Task>, CheckBox, string)>();

            // Popular tab
            if (ChkInstallIDM.IsChecked == true) tasks.Add((() => RunAutomatedProcessAsync(), ChkInstallIDM, "IDM"));
            if (ChkActivateWindows.IsChecked == true) tasks.Add((() => Task.Run(() => ActivateWindows()), ChkActivateWindows, "Activate Windows"));
            if (ChkActivateOffice.IsChecked == true) tasks.Add((() => Task.Run(() => ActivateOffice()), ChkActivateOffice, "Activate Office"));
            if (ChkOfficeToolPlus.IsChecked == true) tasks.Add((InstallOfficeToolPlusAsync, ChkOfficeToolPlus, "Office Tool Plus"));
            if (ChkPauseWindowsUpdate.IsChecked == true) tasks.Add((() => Task.Run(() => PauseWindowsUpdate()), ChkPauseWindowsUpdate, "Pause Windows Update"));
            if (ChkInstallWinRAR.IsChecked == true) tasks.Add((InstallAndActivateWinRARAsync, ChkInstallWinRAR, "WinRAR"));
            if (ChkInstallBID.IsChecked == true) tasks.Add((InstallAndActivateBIDAsync, ChkInstallBID, "BID"));
            if (ChkVcredist.IsChecked == true) tasks.Add((InstallVcredistAsync, ChkVcredist, "Vcredist"));
            if (ChkDirectX.IsChecked == true) tasks.Add((InstallDirectXAsync, ChkDirectX, "DirectX"));
            if (ChkJava.IsChecked == true) tasks.Add((InstallJavaAsync, ChkJava, "Java"));
            if (ChkOpenAL.IsChecked == true) tasks.Add((InstallOpenALAsync, ChkOpenAL, "OpenAL"));
            if (ChkChrome.IsChecked == true) tasks.Add((InstallChromeAsync, ChkChrome, "Chrome"));
            if (ChkCocCoc.IsChecked == true) tasks.Add((InstallCocCocAsync, ChkCocCoc, "CocCoc"));
            if (ChkEdge.IsChecked == true) tasks.Add((InstallEdgeAsync, ChkEdge, "Edge"));
            if (ChkPotPlayer.IsChecked == true) tasks.Add((InstallPotPlayerAsync, ChkPotPlayer, "PotPlayer"));
            if (ChkFastStone.IsChecked == true) tasks.Add((InstallFastStoneAsync, ChkFastStone, "FastStone"));
            if (ChkFoxit.IsChecked == true) tasks.Add((InstallFoxitAsync, ChkFoxit, "Foxit"));
            if (ChkBandiview.IsChecked == true) tasks.Add((InstallBandiviewAsync, ChkBandiview, "Bandiview"));
            if (ChkAdvancedCodecPack.IsChecked == true) tasks.Add((InstallAdvancedCodecPackAsync, ChkAdvancedCodecPack, "Advanced Codec Pack"));
            if (ChkRevoUninstaller.IsChecked == true) tasks.Add((InstallHibitUninstallerAsync, ChkRevoUninstaller, "Revo Uninstaller"));
            if (ChkInstallZalo.IsChecked == true) tasks.Add((InstallZaloAsync, ChkInstallZalo, "Zalo"));
            if (Chk3DPChip.IsChecked == true) tasks.Add((Run3DPChipAsync, Chk3DPChip, "3DP Chip"));
            if (Chk3DPNet.IsChecked == true) tasks.Add((Install3DPNetAsync, Chk3DPNet, "3DP Net"));
            if (ChkMMTApps.IsChecked == true) tasks.Add((InstallMMTAppsAsync, ChkMMTApps, "MMT Apps"));
            if (ChkDISMPP.IsChecked == true) tasks.Add((InstallDISMPPAsync, ChkDISMPP, "DISM++"));
            if (ChkComfortClipboardPro.IsChecked == true) tasks.Add((InstallComfortClipboardProAsync, ChkComfortClipboardPro, "Comfort Clipboard Pro"));
            if (ChkOfficeSoftmaker.IsChecked == true) tasks.Add((InstallOfficeSoftmakerAsync, ChkOfficeSoftmaker, "Office Softmaker"));
            if (ChkGouenjiFonts.IsChecked == true) tasks.Add((InstallGouenjiFontsAsync, ChkGouenjiFonts, "Gouenji Fonts"));
            if (ChkNotepadPlusPlus.IsChecked == true) tasks.Add((InstallNotepadPlusPlusAsync, ChkNotepadPlusPlus, "Notepad++"));

            // System tab
            if (ChkPowerISO.IsChecked == true) tasks.Add((InstallPowerISOAsync, ChkPowerISO, "PowerISO"));
            if (ChkTeraCopy.IsChecked == true) tasks.Add((InstallTeraCopyAsync, ChkTeraCopy, "TeraCopy"));
            if (ChkVPN1111.IsChecked == true) tasks.Add((InstallVPN1111Async, ChkVPN1111, "VPN 1111"));
            if (ChkGoogleDrive.IsChecked == true) tasks.Add((InstallGoogleDriveAsync, ChkGoogleDrive, "Google Drive"));
            if (ChkNetLimiter.IsChecked == true) tasks.Add((InstallNetLimiterAsync, ChkNetLimiter, "NetLimiter"));
            if (ChkFolderSize.IsChecked == true) tasks.Add((InstallFolderSizeAsync, ChkFolderSize, "FolderSize"));

            // Partition tab
            if (ChkDiskGenius.IsChecked == true) tasks.Add((InstallDiskGeniusAsync, ChkDiskGenius, "DiskGenius"));
            if (ChkAomeiPartitionAssistant.IsChecked == true) tasks.Add((InstallAomeiPartitionAssistantAsync, ChkAomeiPartitionAssistant, "AOMEI"));

            // Gaming tab
            if (ChkProcessLasso.IsChecked == true) tasks.Add((InstallProcessLassoAsync, ChkProcessLasso, "Process Lasso"));
            if (ChkThrottlestop.IsChecked == true) tasks.Add((InstallThrottlestopAsync, ChkThrottlestop, "Throttlestop"));
            if (ChkMSIAfterburner.IsChecked == true) tasks.Add((InstallMSIAfterburnerAsync, ChkMSIAfterburner, "MSI Afterburner"));
            if (ChkLeagueOfLegends.IsChecked == true) tasks.Add((InstallLeagueOfLegendsVNAsync, ChkLeagueOfLegends, "League of Legends"));
            if (ChkPorofessor.IsChecked == true) tasks.Add((InstallPorofessorAsync, ChkPorofessor, "Porofessor"));
            if (ChkSamuraiMaiden.IsChecked == true) tasks.Add((InstallSamuraiMaidenAsync, ChkSamuraiMaiden, "Samurai Maiden"));
            if (ChkGhostOfTsushima.IsChecked == true) tasks.Add((InstallGhostOfTsushimaAsync, ChkGhostOfTsushima, "Ghost of Tsushima"));

            // Remote Desktop tab
            if (ChkUltraviewer.IsChecked == true) tasks.Add((InstallUltraviewerAsync, ChkUltraviewer, "UltraViewer"));
            if (ChkTeamViewerQS.IsChecked == true) tasks.Add((InstallTeamViewerQuickSupportAsync, ChkTeamViewerQS, "TeamViewer QS"));
            if (ChkTeamViewerFull.IsChecked == true) tasks.Add((InstallTeamViewerFullAsync, ChkTeamViewerFull, "TeamViewer Full"));
            if (ChkAnyDesk.IsChecked == true) tasks.Add((InstallAnyDeskAsync, ChkAnyDesk, "AnyDesk"));
            if (ChkVMWare162Lite.IsChecked == true) tasks.Add((InstallVMWare162LiteAsync, ChkVMWare162Lite, "VMWare 16.2 Lite"));

            // Windows tabs
            if (ChkWin11_26H1.IsChecked == true) tasks.Add((InstallWin11_26H1Async, ChkWin11_26H1, "Windows 11 26H1"));
            if (ChkWin10LtscIot21H2.IsChecked == true) tasks.Add((InstallWin10LtscIot21H2Async, ChkWin10LtscIot21H2, "Windows 10 LTSC IoT"));

            return tasks;
        }

        /// <summary>
        /// Executes tasks with multi-task queue support
        /// Tasks can be added dynamically while others are running
        /// </summary>
        private async Task ExecuteTaskQueueAsync(List<(Func<Task> Action, CheckBox CheckBox, string Name)> tasks)
        {
            if (tasks.Count == 0)
            {
                UpdateStatus("No tasks selected", "Yellow");
                return;
            }

            UpdateStatus($"Starting {tasks.Count} task(s)...", "Cyan");
            
            var failedTasks = new List<string>();
            var completedTasks = new List<string>();

            for (int i = 0; i < tasks.Count; i++)
            {
                var taskInfo = tasks[i];
                var currentTaskCheckBox = taskInfo.CheckBox;

                // Check for cancellation
                if (_cancellationTokenSource?.Token.IsCancellationRequested == true)
                {
                    UpdateStatus("Installation cancelled by user", "Red");
                    break;
                }

                // Delay between tasks to avoid server overload
                if (tasks.Count > 1 && i > 0)
                {
                    await Task.Delay(500, _cancellationTokenSource?.Token ?? CancellationToken.None);
                }

                try
                {
                    UpdateStatus($"[{i + 1}/{tasks.Count}] Installing: {taskInfo.Name}...", "Cyan");
                    
                    // Execute the task
                    await taskInfo.Action();
                    
                    // Mark as completed
                    completedTasks.Add(taskInfo.Name);
                    
                    if (currentTaskCheckBox != null)
                    {
                        currentTaskCheckBox.IsChecked = false;
                        currentTaskCheckBox.Foreground = new SolidColorBrush(Colors.Cyan);
                    }
                    
                    UpdateStatus($"✓ Completed: {taskInfo.Name}", "Green");
                }
                catch (OperationCanceledException)
                {
                    UpdateStatus($"Cancelled: {taskInfo.Name}", "Yellow");
                    if (currentTaskCheckBox != null)
                    {
                        currentTaskCheckBox.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    break;
                }
                catch (Exception ex)
                {
                    failedTasks.Add($"{taskInfo.Name}: {ex.Message}");
                    UpdateStatus($"✗ Failed: {taskInfo.Name} - {ex.Message}", "Red");
                    
                    if (currentTaskCheckBox != null)
                    {
                        currentTaskCheckBox.Foreground = new SolidColorBrush(Colors.Red);
                    }
                    
                    // Continue with next task instead of stopping completely
                    // This allows partial success
                }
            }

            // Summary
            if (failedTasks.Count > 0)
            {
                UpdateStatus($"Completed: {completedTasks.Count}, Failed: {failedTasks.Count}", "Orange");
                Debug.WriteLine("Failed tasks: " + string.Join("; ", failedTasks));
            }
            else
            {
                UpdateStatus($"All {completedTasks.Count} task(s) completed successfully!", "Green");
            }
        }
    }
}
