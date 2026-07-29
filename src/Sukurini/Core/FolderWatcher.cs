using System;
using System.IO;
using System.Threading;
using Sukurini.Infrastructure;

namespace Sukurini.Core;

public interface IFolderWatcher : IDisposable
{
    void Start();
    void Cancel();
}

public class FolderWatcher : IFolderWatcher
{
    private readonly string _folderPath;
    private readonly TimeSpan _debounceInterval;
    private readonly Action _onChange;
    private readonly Action _onFolderLost;
    private readonly bool _includeSubdirectories;

    private FileSystemWatcher? _watcher;
    private Timer? _debounceTimer;
    private Timer? _existenceTimer;
    private bool _isStarted;
    private bool _isDisposed;
    private bool _didReportLost;
    private readonly object _lock = new();

    public FolderWatcher(
        string folderPath,
        TimeSpan debounceInterval,
        Action onChange,
        Action onFolderLost,
        bool includeSubdirectories = false)
    {
        _folderPath = Path.GetFullPath(folderPath);
        _debounceInterval = debounceInterval;
        _onChange = onChange;
        _onFolderLost = onFolderLost;
        _includeSubdirectories = includeSubdirectories;
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_isStarted || _isDisposed) return;
            _isStarted = true;

            if (!Directory.Exists(_folderPath))
            {
                Log.Watcher.Error($"Start failed: Directory does not exist path={_folderPath}");
                ReportFolderLost("directory_not_found");
                return;
            }

            try
            {
                _watcher = new FileSystemWatcher(_folderPath)
                {
                    IncludeSubdirectories = _includeSubdirectories,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
                };

                _watcher.Created += OnFileSystemEvent;
                _watcher.Changed += OnFileSystemEvent;
                _watcher.Deleted += OnFileSystemEvent;
                _watcher.Renamed += OnFileSystemEvent;
                _watcher.Error += OnWatcherError;

                _watcher.EnableRaisingEvents = true;

                // 2초 간격으로 폴더 존재 여부 주기적 확인 (폴더 삭제/소실 감지)
                _existenceTimer = new Timer(CheckFolderExistence, null, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));

                Log.Watcher.Info($"Started watching path={_folderPath} includeSubdirs={_includeSubdirectories}");
            }
            catch (Exception ex)
            {
                Log.Watcher.Error($"Failed to initialize FileSystemWatcher: {ex.Message}");
                ReportFolderLost("init_exception");
            }
        }
    }

    private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
    {
        ScheduleChange();
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        Log.Watcher.Warn($"FileSystemWatcher error: {e.GetException().Message}");
        ScheduleChange();
    }

    private void ScheduleChange()
    {
        lock (_lock)
        {
            if (_isDisposed) return;

            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ =>
            {
                if (_isDisposed) return;
                Log.Watcher.Debug($"Folder change event fired path={_folderPath}");
                _onChange?.Invoke();
            }, null, (int)_debounceInterval.TotalMilliseconds, Timeout.Infinite);
        }
    }

    private void CheckFolderExistence(object? state)
    {
        if (_isDisposed) return;

        if (!Directory.Exists(_folderPath))
        {
            ReportFolderLost("folder_deleted");
            Cancel();
        }
    }

    private void ReportFolderLost(string reason)
    {
        lock (_lock)
        {
            if (_didReportLost) return;
            _didReportLost = true;
            Log.Watcher.Error($"Folder lost path={_folderPath} reason={reason}");
            _onFolderLost?.Invoke();
        }
    }

    public void Cancel()
    {
        Dispose();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_isDisposed) return;
            _isDisposed = true;

            _debounceTimer?.Dispose();
            _debounceTimer = null;

            _existenceTimer?.Dispose();
            _existenceTimer = null;

            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Created -= OnFileSystemEvent;
                _watcher.Changed -= OnFileSystemEvent;
                _watcher.Deleted -= OnFileSystemEvent;
                _watcher.Renamed -= OnFileSystemEvent;
                _watcher.Error -= OnWatcherError;
                _watcher.Dispose();
                _watcher = null;
            }

            Log.Watcher.Info($"Watcher cancelled path={_folderPath}");
        }
    }
}
