using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrayShot.Infrastructure;
using TrayShot.Models;

namespace TrayShot.Core;

public enum ConversionOrigin
{
    Live,
    Backfill
}

public record ConversionHold(
    Guid Token,
    string SourcePath,
    string DestinationPath,
    ConversionOrigin Origin,
    DateTime CreatedAt)
{
    public bool IsExpired => Origin == ConversionOrigin.Live
        ? (DateTime.UtcNow - CreatedAt).TotalSeconds > 12
        : (DateTime.UtcNow - CreatedAt).TotalSeconds > 45;
}

public sealed class ScreenshotStore : IDisposable
{
    private static readonly Lazy<ScreenshotStore> _instance = new(() => new ScreenshotStore());
    public static ScreenshotStore Shared => _instance.Value;

    private readonly SemaphoreSlim _scanSemaphore = new(1, 1);
    private readonly object _stateLock = new();
    private readonly ConcurrentDictionary<Guid, ConversionHold> _conversionHolds = new();

    private List<Screenshot> _items = new();
    private string? _activeFolderPath;
    private bool _includeSubfolders;
    private IFolderWatcher? _watcher;
    private bool _isDisposed;

    public event Action<StoreChange>? Changed;

    public IReadOnlyList<Screenshot> Items
    {
        get
        {
            lock (_stateLock)
            {
                return _items.ToList();
            }
        }
    }

    public string? ActiveFolderPath
    {
        get
        {
            lock (_stateLock)
            {
                return _activeFolderPath;
            }
        }
    }

    public ScreenshotStore()
    {
    }

    public void Initialize(string activeFolderPath, bool includeSubfolders)
    {
        lock (_stateLock)
        {
            _activeFolderPath = activeFolderPath;
            _includeSubfolders = includeSubfolders;
        }

        RestartWatcher();
        TriggerScan();
    }

    public void SetActiveFolder(string folderPath, bool includeSubfolders)
    {
        lock (_stateLock)
        {
            if (_activeFolderPath == folderPath && _includeSubfolders == includeSubfolders)
                return;

            _activeFolderPath = folderPath;
            _includeSubfolders = includeSubfolders;
        }

        RestartWatcher();
        TriggerScan();
    }

    private void RestartWatcher()
    {
        _watcher?.Dispose();
        _watcher = null;

        string? folder;
        bool subfolders;
        lock (_stateLock)
        {
            folder = _activeFolderPath;
            subfolders = _includeSubfolders;
        }

        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
        {
            _watcher = new FolderWatcher(
                folder,
                TimeSpan.FromMilliseconds(200),
                onChange: () => TriggerScan(),
                onFolderLost: () => OnFolderLost(),
                includeSubdirectories: subfolders);
            _watcher.Start();
        }
    }

    private void OnFolderLost()
    {
        Log.Store.Error($"Active folder lost path={_activeFolderPath}");
        lock (_stateLock)
        {
            _items.Clear();
        }
        Changed?.Invoke(new StoreChange(Array.Empty<Screenshot>(), new HashSet<string>(), true, new Dictionary<string, string>()));
    }

    public void TriggerScan()
    {
        Task.Run(async () => await PerformScanAsync());
    }

    private async Task PerformScanAsync()
    {
        if (_isDisposed) return;
        await _scanSemaphore.WaitAsync();
        try
        {
            string? folder;
            bool subfolders;
            lock (_stateLock)
            {
                folder = _activeFolderPath;
                subfolders = _includeSubfolders;
            }

            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder))
            {
                lock (_stateLock)
                {
                    if (_items.Count > 0)
                    {
                        _items.Clear();
                        Changed?.Invoke(new StoreChange(Array.Empty<Screenshot>(), new HashSet<string>(), true, new Dictionary<string, string>()));
                    }
                }
                return;
            }

            var scannedFiles = ScanDirectory(folder, subfolders);
            var settledFiles = await SettleFilesAsync(scannedFiles);

            List<Screenshot> oldItems;
            lock (_stateLock)
            {
                oldItems = _items.ToList();
            }

            var oldMap = oldItems.ToDictionary(i => i.Path, StringComparer.OrdinalIgnoreCase);
            var newMap = settledFiles.ToDictionary(i => i.Path, StringComparer.OrdinalIgnoreCase);

            var inserted = settledFiles.Where(f => !oldMap.ContainsKey(f.Path)).ToList();
            var removedPaths = oldItems.Where(f => !newMap.ContainsKey(f.Path)).Select(f => f.Path).ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Filter out files held during active conversion
            CleanExpiredConversionHolds();
            var activeHoldSources = _conversionHolds.Values.Select(h => h.SourcePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var filteredInserted = inserted.Where(f => !activeHoldSources.Contains(f.Path)).ToList();

            lock (_stateLock)
            {
                _items = settledFiles.OrderByDescending(f => f.Created).ToList();
            }

            if (filteredInserted.Count > 0 || removedPaths.Count > 0)
            {
                var change = new StoreChange(filteredInserted, removedPaths, false, new Dictionary<string, string>());
                Log.Store.Info($"Store updated inserted={filteredInserted.Count} removed={removedPaths.Count} total={_items.Count}");
                Changed?.Invoke(change);
            }
        }
        catch (Exception ex)
        {
            Log.Store.Error($"Scan error: {ex.Message}");
        }
        finally
        {
            _scanSemaphore.Release();
        }
    }

    private List<Screenshot> ScanDirectory(string dirPath, bool subfolders)
    {
        var result = new List<Screenshot>();
        try
        {
            var searchOption = subfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var dirInfo = new DirectoryInfo(dirPath);
            foreach (var file in dirInfo.EnumerateFiles("*.*", searchOption))
            {
                if (ScreenshotFile.IsEligible(file.FullName))
                {
                    result.Add(new Screenshot(file.FullName, ScreenshotFile.GetEffectiveCreatedTime(file), file.Length));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Store.Warn($"Directory enumeration error path={dirPath}: {ex.Message}");
        }
        return result;
    }

    private async Task<List<Screenshot>> SettleFilesAsync(List<Screenshot> candidates)
    {
        // 다단계 파일크기 안정화 검사 (File Settle Ladder: 0.1s -> 0.3s -> 0.6s)
        var settled = new List<Screenshot>();
        double[] delays = { 0.1, 0.3, 0.6 };

        foreach (var sc in candidates)
        {
            bool isReady = false;
            for (int i = 0; i < delays.Length; i++)
            {
                if (IsFileSettledAndReadable(sc.Path))
                {
                    isReady = true;
                    break;
                }
                await Task.Delay((int)(delays[i] * 1000));
            }
            if (isReady)
            {
                settled.Add(sc);
            }
        }
        return settled;
    }

    private bool IsFileSettledAndReadable(string path)
    {
        try
        {
            if (!File.Exists(path)) return false;
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            return fs.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public Guid AddConversionHold(string sourcePath, string destinationPath, ConversionOrigin origin)
    {
        var token = Guid.NewGuid();
        var hold = new ConversionHold(token, sourcePath, destinationPath, origin, DateTime.UtcNow);
        _conversionHolds[token] = hold;
        Log.Store.Debug($"Conversion hold added source={sourcePath} token={token}");
        return token;
    }

    public void RemoveConversionHold(Guid token)
    {
        _conversionHolds.TryRemove(token, out _);
        Log.Store.Debug($"Conversion hold removed token={token}");
    }

    private void CleanExpiredConversionHolds()
    {
        foreach (var kvp in _conversionHolds)
        {
            if (kvp.Value.IsExpired)
            {
                _conversionHolds.TryRemove(kvp.Key, out _);
                Log.Store.Warn($"Conversion hold expired token={kvp.Key} source={kvp.Value.SourcePath}");
            }
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _watcher?.Dispose();
        _scanSemaphore.Dispose();
    }
}
