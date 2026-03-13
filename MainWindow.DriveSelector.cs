// =======================================================================
// MainWindow.DriveSelector.cs
// Chức năng: Drive ComboBox, auto path routing, multi-task queue
// Cập nhật: 2026-03-12 - Refactored for concurrency & state management
// =======================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GMTPC.Tool.Services;

namespace GMTPC.Tool
{
    public partial class MainWindow
    {
        // Drive scanner fields
        private List<DriveInfoEx> _detectedDrives;
        private bool _isScanningDrives;

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
                _detectedDrives = await DriveScannerService.ScanDrivesAsync();
                
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
