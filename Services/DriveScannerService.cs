// =============================================================================
// Services/DriveScannerService.cs
// Async drive detection with UI thread safety
// Filters out CD/DVD drives, displays [Drive Letter/Name] - [Free Space]
// Optimized for systems with multiple drive types (SSD, NVMe, HDD)
// UTF-8 with BOM – .NET Framework 4.8 / C# 7.3
// =============================================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace GMTPC.Tool.Services
{
    /// <summary>
    /// Represents information about a detected drive
    /// </summary>
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

    /// <summary>
    /// Service for scanning and detecting drives asynchronously
    /// Thread-safe for UI binding
    /// </summary>
    public static class DriveScannerService
    {
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
        /// Formats bytes to human-readable format
        /// </summary>
        private static string FormatBytes(long bytes)
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
    }
}
