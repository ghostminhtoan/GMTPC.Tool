// =============================================================================
// Services/DownloadConfiguration.cs
// Global download path configuration and task isolation
// Provides base temp directory and isolated subfolders for each download task
// UTF-8 with BOM – .NET Framework 4.8 / C# 7.3
// =============================================================================
using System;
using System.IO;

namespace GMTPC.Tool.Services
{
    /// <summary>
    /// Global download configuration service
    /// Provides thread-safe access to the selected temp drive and creates isolated subfolders
    /// </summary>
    public static class DownloadConfiguration
    {
        private static string _selectedTempPath = null;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets or sets the global temp base path (e.g., K:\temp)
        /// This is the root folder for ALL downloads
        /// </summary>
        public static string TempBasePath
        {
            get
            {
                lock (_lock)
                {
                    return _selectedTempPath;
                }
            }
            set
            {
                lock (_lock)
                {
                    _selectedTempPath = value;
                }
            }
        }

        /// <summary>
        /// Gets the isolated download folder for a specific task
        /// Format: {TempBasePath}\{TaskName}\
        /// Example: K:\temp\Process Lasso\
        /// </summary>
        /// <param name="taskName">Name of the download task (used as subfolder name)</param>
        /// <returns>Full path to the isolated task folder</returns>
        public static string GetTaskDownloadFolder(string taskName)
        {
            string basePath;
            lock (_lock)
            {
                basePath = _selectedTempPath;
            }

            if (string.IsNullOrEmpty(basePath))
            {
                // Fallback to GMTPC folder if no temp path selected
                return Path.Combine(GetGMTPCFolder(), SanitizeFolderName(taskName));
            }

            // Create isolated subfolder: {Drive}:\temp\{TaskName}\
            string taskFolder = Path.Combine(basePath, SanitizeFolderName(taskName));
            
            // Ensure the folder exists
            if (!Directory.Exists(taskFolder))
            {
                Directory.CreateDirectory(taskFolder);
            }

            return taskFolder;
        }

        /// <summary>
        /// Gets the GMTPC base folder (fallback location)
        /// </summary>
        private static string GetGMTPCFolder()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string gmtpcFolder = Path.Combine(appData, "GMTPC");
            
            if (!Directory.Exists(gmtpcFolder))
            {
                Directory.CreateDirectory(gmtpcFolder);
            }
            
            return gmtpcFolder;
        }

        /// <summary>
        /// Sanitizes a string to be used as a folder name
        /// Removes or replaces invalid characters
        /// </summary>
        private static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return "Unknown";
            }

            // Remove invalid filename characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] invalidPathChars = Path.GetInvalidPathChars();
            
            string sanitized = name;
            foreach (char c in invalidChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }
            foreach (char c in invalidPathChars)
            {
                sanitized = sanitized.Replace(c, '_');
            }

            // Trim whitespace
            sanitized = sanitized.Trim();

            // Ensure not empty after sanitization
            if (string.IsNullOrEmpty(sanitized))
            {
                return "Download";
            }

            return sanitized;
        }

        /// <summary>
        /// Sets the temp base path and ensures the folder exists
        /// Called when user selects a drive from ComboBox
        /// </summary>
        /// <param name="driveLetter">Selected drive letter (e.g., "K")</param>
        /// <returns>The created temp base path</returns>
        public static string SetTempBasePath(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter))
            {
                throw new ArgumentNullException(nameof(driveLetter));
            }

            var letter = driveLetter.TrimEnd(':', '\\');
            var tempPath = Path.Combine(letter + ":", "temp");

            try
            {
                // Create base temp folder
                if (!Directory.Exists(tempPath))
                {
                    Directory.CreateDirectory(tempPath);
                }

                TempBasePath = tempPath;
                return tempPath;
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new UnauthorizedAccessException(
                    $"Insufficient permissions to create temp folder at {tempPath}. " +
                    $"Please run as administrator or choose a different drive.", ex);
            }
            catch (IOException ex)
            {
                throw new IOException(
                    $"Failed to create temp folder at {tempPath}: {ex.Message}", ex);
            }
        }
    }
}
