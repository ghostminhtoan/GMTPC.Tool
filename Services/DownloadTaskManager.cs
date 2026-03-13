// =============================================================================
// Services/DownloadTaskManager.cs
// Thread-safe multi-task download queue manager with concurrency control
// UTF-8 with BOM – .NET Framework 4.8 / C# 7.3
// =============================================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GMTPC.Tool.Services
{
    /// <summary>
    /// Represents the state of a download task
    /// </summary>
    public enum DownloadTaskState
    {
        Pending,
        Running,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    /// <summary>
    /// Represents a single download task with state management
    /// </summary>
    public class DownloadTask
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
        public string DownloadUrl { get; set; }
        public string DestinationPath { get; set; }
        public string InstallArguments { get; set; }
        public Func<Task> InstallAction { get; set; }
        
        private DownloadTaskState _state;
        private readonly object _stateLock = new object();
        
        public DownloadTaskState State
        {
            get
            {
                lock (_stateLock) return _state;
            }
            set
            {
                lock (_stateLock) _state = value;
            }
        }
        
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public Exception LastException { get; set; }
        public int RetryCount { get; set; }
        public int MaxRetries { get; set; } = 3;
        
        public DownloadTask(string id, string displayName, string url, string destPath, Func<Task> installAction = null)
        {
            Id = id;
            DisplayName = displayName;
            DownloadUrl = url;
            DestinationPath = destPath;
            InstallAction = installAction;
            State = DownloadTaskState.Pending;
            CancellationTokenSource = new CancellationTokenSource();
        }
    }

    /// <summary>
    /// Manages a queue of download tasks with concurrent execution support
    /// Thread-safe for UI interactions during downloads
    /// </summary>
    public sealed class DownloadTaskManager : IDisposable
    {
        private readonly ConcurrentQueue<DownloadTask> _pendingQueue;
        private readonly ConcurrentDictionary<string, DownloadTask> _activeTasks;
        private readonly ConcurrentDictionary<string, DownloadTask> _completedTasks;
        private readonly SemaphoreSlim _concurrencySemaphore;
        private readonly object _globalLock = new object();
        
        private CancellationTokenSource _globalCancellationTokenSource;
        private CancellationTokenSource _pauseCancellationTokenSource;
        private ManualResetEventSlim _pauseEvent;
        
        private bool _isRunning;
        private bool _isPaused;
        private bool _isDisposed;
        private int _maxConcurrency;

        public event Action<DownloadTask> OnTaskStateChanged;
        public event Action<string> OnStatusMessage;
        public event Action<bool> OnGlobalStateChanged;

        public bool IsRunning
        {
            get { lock (_globalLock) return _isRunning; }
            private set { lock (_globalLock) _isRunning = value; }
        }

        public bool IsPaused
        {
            get { lock (_globalLock) return _isPaused; }
            private set { lock (_globalLock) _isPaused = value; }
        }

        public int ActiveTaskCount => _activeTasks.Count;
        public int PendingTaskCount => _pendingQueue.Count;
        public int CompletedTaskCount => _completedTasks.Count;
        public int MaxConcurrency
        {
            get => _maxConcurrency;
            set
            {
                if (value < 1) value = 1;
                _maxConcurrency = value;
                _concurrencySemaphore.Dispose();
                // Note: Semaphore can't be recreated easily, this is a limitation
            }
        }

        public DownloadTaskManager(int maxConcurrency = 1)
        {
            _maxConcurrency = maxConcurrency;
            _pendingQueue = new ConcurrentQueue<DownloadTask>();
            _activeTasks = new ConcurrentDictionary<string, DownloadTask>();
            _completedTasks = new ConcurrentDictionary<string, DownloadTask>();
            _concurrencySemaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            _pauseEvent = new ManualResetEventSlim(true);
            _globalCancellationTokenSource = new CancellationTokenSource();
            _pauseCancellationTokenSource = new CancellationTokenSource();
            IsRunning = false;
            IsPaused = false;
        }

        /// <summary>
        /// Add a task to the pending queue
        /// Thread-safe: Can be called while downloads are running
        /// </summary>
        public void AddTask(DownloadTask task)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(DownloadTaskManager));
            if (task == null) throw new ArgumentNullException(nameof(task));
            
            _pendingQueue.Enqueue(task);
            task.State = DownloadTaskState.Pending;
            
            OnTaskStateChanged?.Invoke(task);
            
            // If not running and not paused, start processing
            if (!IsRunning && !IsPaused)
            {
                StartProcessing();
            }
        }

        /// <summary>
        /// Add multiple tasks to the queue
        /// </summary>
        public void AddTasks(IEnumerable<DownloadTask> tasks)
        {
            foreach (var task in tasks)
            {
                AddTask(task);
            }
        }

        /// <summary>
        /// Start processing the queue
        /// </summary>
        private async void StartProcessing()
        {
            if (IsRunning) return;
            
            IsRunning = true;
            IsPaused = false;
            _pauseEvent.Set();
            
            OnGlobalStateChanged?.Invoke(true);
            
            try
            {
                var workerTasks = new List<Task>();
                
                // Start worker tasks up to max concurrency
                for (int i = 0; i < _maxConcurrency; i++)
                {
                    workerTasks.Add(Task.Run(() => ProcessQueueAsync(_globalCancellationTokenSource.Token)));
                }
                
                await Task.WhenAll(workerTasks);
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
            catch (Exception ex)
            {
                OnStatusMessage?.Invoke($"Error in download manager: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                OnGlobalStateChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// Process tasks from the queue
        /// </summary>
        private async Task ProcessQueueAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (_pendingQueue.TryDequeue(out var task))
                {
                    await _concurrencySemaphore.WaitAsync(cancellationToken);
                    
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            task.State = DownloadTaskState.Cancelled;
                            _pendingQueue.Enqueue(task); // Re-queue for potential retry
                            break;
                        }
                        
                        _activeTasks.TryAdd(task.Id, task);
                        await ExecuteTaskAsync(task, cancellationToken);
                    }
                    finally
                    {
                        _activeTasks.TryRemove(task.Id, out _);
                        _concurrencySemaphore.Release();
                    }
                }
                else
                {
                    // No more tasks, exit worker
                    break;
                }
            }
        }

        /// <summary>
        /// Execute a single download task with retry logic
        /// </summary>
        private async Task ExecuteTaskAsync(DownloadTask task, CancellationToken globalCt)
        {
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                globalCt, 
                task.CancellationTokenSource.Token,
                _pauseCancellationTokenSource.Token
            );
            
            try
            {
                task.State = DownloadTaskState.Running;
                OnTaskStateChanged?.Invoke(task);
                
                // Wait for pause event before starting
                await Task.Run(() => _pauseEvent.Wait(linkedCts.Token), linkedCts.Token);
                
                if (task.InstallAction != null)
                {
                    await task.InstallAction();
                }
                
                task.State = DownloadTaskState.Completed;
                _completedTasks.TryAdd(task.Id, task);
                OnTaskStateChanged?.Invoke(task);
                OnStatusMessage?.Invoke($"Completed: {task.DisplayName}");
            }
            catch (OperationCanceledException)
            {
                if (globalCt.IsCancellationRequested || _pauseCancellationTokenSource.IsCancellationRequested)
                {
                    task.State = DownloadTaskState.Cancelled;
                    OnStatusMessage?.Invoke($"Cancelled: {task.DisplayName}");
                }
                else if (IsPaused)
                {
                    task.State = DownloadTaskState.Paused;
                    _pendingQueue.Enqueue(task); // Re-queue for resume
                }
                OnTaskStateChanged?.Invoke(task);
            }
            catch (Exception ex)
            {
                task.LastException = ex;
                task.RetryCount++;
                
                if (task.RetryCount < task.MaxRetries)
                {
                    OnStatusMessage?.Invoke($"Retry {task.RetryCount}/{task.MaxRetries}: {task.DisplayName}");
                    _pendingQueue.Enqueue(task); // Re-queue for retry
                }
                else
                {
                    task.State = DownloadTaskState.Failed;
                    _completedTasks.TryAdd(task.Id, task);
                    OnStatusMessage?.Invoke($"Failed: {task.DisplayName} - {ex.Message}");
                }
                OnTaskStateChanged?.Invoke(task);
            }
            finally
            {
                linkedCts.Dispose();
            }
        }

        /// <summary>
        /// Pause all running tasks
        /// </summary>
        public void Pause()
        {
            if (_isDisposed) return;
            if (IsPaused) return;
            
            IsPaused = true;
            _pauseEvent.Reset();
            _pauseCancellationTokenSource.Cancel();
            _pauseCancellationTokenSource = new CancellationTokenSource();
            
            OnStatusMessage?.Invoke("Paused all downloads");
            OnGlobalStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Resume all paused tasks
        /// </summary>
        public void Resume()
        {
            if (_isDisposed) return;
            if (!IsPaused) return;
            
            IsPaused = false;
            _pauseEvent.Set();
            
            OnStatusMessage?.Invoke("Resumed all downloads");
            OnGlobalStateChanged?.Invoke(true);
            
            // Restart processing if there are pending tasks
            if (_pendingQueue.Count > 0 && !IsRunning)
            {
                StartProcessing();
            }
        }

        /// <summary>
        /// Stop all tasks and clear the queue
        /// </summary>
        public void Stop()
        {
            if (_isDisposed) return;
            
            // Cancel all active tasks
            _globalCancellationTokenSource.Cancel();
            
            // Cancel all pending task CTS
            foreach (var task in _activeTasks.Values)
            {
                try { task.CancellationTokenSource.Cancel(); } catch { }
            }
            
            // Clear pending queue
            while (_pendingQueue.TryDequeue(out _)) { }
            
            IsRunning = false;
            IsPaused = false;
            _pauseEvent.Set();
            
            // Reset global cancellation token
            _globalCancellationTokenSource.Dispose();
            _globalCancellationTokenSource = new CancellationTokenSource();
            
            OnStatusMessage?.Invoke("Stopped all downloads");
            OnGlobalStateChanged?.Invoke(false);
        }

        /// <summary>
        /// Get all tasks (pending, active, completed)
        /// </summary>
        public IEnumerable<DownloadTask> GetAllTasks()
        {
            foreach (var task in _pendingQueue) yield return task;
            foreach (var kvp in _activeTasks) yield return kvp.Value;
            foreach (var kvp in _completedTasks) yield return kvp.Value;
        }

        /// <summary>
        /// Get tasks by state
        /// </summary>
        public IEnumerable<DownloadTask> GetTasksByState(DownloadTaskState state)
        {
            foreach (var task in GetAllTasks())
            {
                if (task.State == state) yield return task;
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            
            _isDisposed = true;
            Stop();
            _globalCancellationTokenSource?.Dispose();
            _pauseCancellationTokenSource?.Dispose();
            _pauseEvent?.Dispose();
            _concurrencySemaphore?.Dispose();
        }
    }
}
