// =============================================================================
// Services/DownloadRegistry.cs
// Global registry for tracking all active downloads across all tabs
// Thread-safe using ConcurrentDictionary
// UTF-8 with BOM – .NET Framework 4.8 / C# 7.3
// =============================================================================
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace GMTPC.Tool.Services
{
    /// <summary>
    /// Represents the state of a registered download task
    /// </summary>
    public class DownloadTaskContext
    {
        public string TaskName { get; set; }
        public string DestinationPath { get; set; }
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ManualResetEventSlim PauseEvent { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsPaused { get; set; }
    }

    /// <summary>
    /// Global registry for all active downloads
    /// Allows Pause/Stop to work across all tabs without scanning Visual Tree
    /// </summary>
    public static class DownloadRegistry
    {
        private static readonly ConcurrentDictionary<string, DownloadTaskContext> _activeDownloads
            = new ConcurrentDictionary<string, DownloadTaskContext>();

        /// <summary>
        /// Register a new download task
        /// </summary>
        public static void Register(string taskId, DownloadTaskContext context)
        {
            _activeDownloads.TryAdd(taskId, context);
        }

        /// <summary>
        /// Unregister a completed/failed download task
        /// </summary>
        public static void Unregister(string taskId)
        {
            _activeDownloads.TryRemove(taskId, out _);
        }

        /// <summary>
        /// Pause ALL active downloads across all tabs
        /// </summary>
        public static void PauseAll()
        {
            foreach (var kvp in _activeDownloads)
            {
                try
                {
                    var context = kvp.Value;
                    if (context != null && !context.IsPaused)
                    {
                        context.PauseEvent?.Reset();
                        context.IsPaused = true;
                    }
                }
                catch { /* Ignore individual task errors */ }
            }
        }

        /// <summary>
        /// Resume ALL paused downloads across all tabs
        /// </summary>
        public static void ResumeAll()
        {
            foreach (var kvp in _activeDownloads)
            {
                try
                {
                    var context = kvp.Value;
                    if (context != null && context.IsPaused)
                    {
                        context.PauseEvent?.Set();
                        context.IsPaused = false;
                    }
                }
                catch { /* Ignore individual task errors */ }
            }
        }

        /// <summary>
        /// Stop ALL active downloads across all tabs
        /// </summary>
        public static void StopAll()
        {
            foreach (var kvp in _activeDownloads)
            {
                try
                {
                    var context = kvp.Value;
                    if (context?.CancellationTokenSource != null 
                        && !context.CancellationTokenSource.IsCancellationRequested)
                    {
                        context.CancellationTokenSource.Cancel();
                        context.PauseEvent?.Set(); // Unblock any waiting threads
                    }
                }
                catch { /* Ignore individual task errors */ }
            }
        }

        /// <summary>
        /// Get count of active downloads
        /// </summary>
        public static int ActiveCount => _activeDownloads.Count;

        /// <summary>
        /// Clear all entries (called on app shutdown)
        /// </summary>
        public static void Clear()
        {
            _activeDownloads.Clear();
        }
    }
}
