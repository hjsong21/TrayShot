using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using SixLabors.ImageSharp;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sukurini.Core;
using Sukurini.Infrastructure;
using Sukurini.Models;

namespace Sukurini.Gallery;

public partial class GalleryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _hasSearchQuery;

    [ObservableProperty]
    private Screenshot? _selectedItem;

    [ObservableProperty]
    private bool _isNotPng = true;
    [ObservableProperty]
    private bool _isNotJpg = true;
    [ObservableProperty]
    private bool _isNotWebp = true;
    [ObservableProperty]
    private bool _isNotBmp = true;
    [ObservableProperty]
    private bool _isNotGif = true;
    [ObservableProperty]
    private bool _isNotTiff = true;
    [ObservableProperty]
    private bool _isNotHeic = true;

    [ObservableProperty]
    private bool _isEmptyState;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isStatusVisible;

    private CancellationTokenSource? _statusMessageCts;

    public void ShowStatus(string message, int autoHideMs = 0)
    {
        _statusMessageCts?.Cancel();
        _statusMessageCts = new CancellationTokenSource();
        var token = _statusMessageCts.Token;

        StatusMessage = message;
        IsStatusVisible = true;

        if (autoHideMs > 0)
        {
            Task.Delay(autoHideMs, token).ContinueWith(t =>
            {
                if (!t.IsCanceled)
                {
                    App.Current.Dispatcher.Invoke(() => IsStatusVisible = false);
                }
            }, TaskScheduler.Default);
        }
    }

    private readonly DispatcherTimer _searchDebounceTimer;

    public ObservableCollection<Screenshot> FilteredScreenshots { get; } = new();

    public ObservableCollection<DateGroup> GroupedScreenshots { get; } = new();

    public event Action<Screenshot>? OpenPreviewRequested;

    public GalleryViewModel()
    {
        _searchDebounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(200)
        };
        _searchDebounceTimer.Tick += OnSearchDebounceTick;

        ScreenshotStore.Shared.Changed += OnStoreChanged;
        RefreshList();
    }

    partial void OnSearchQueryChanged(string value)
    {
        HasSearchQuery = !string.IsNullOrEmpty(value);
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
    }

    partial void OnSelectedItemChanged(Screenshot? value)
    {
        if (value != null && !string.IsNullOrEmpty(value.Path))
        {
            string ext = Path.GetExtension(value.Path).ToLowerInvariant();
            IsNotPng = ext != ".png";
            IsNotJpg = ext != ".jpg" && ext != ".jpeg";
            IsNotWebp = ext != ".webp";
            IsNotBmp = ext != ".bmp";
            IsNotGif = ext != ".gif";
            IsNotTiff = ext != ".tiff" && ext != ".tif";
            IsNotHeic = ext != ".heic";
        }
        else
        {
            IsNotPng = IsNotJpg = IsNotWebp = IsNotBmp = IsNotGif = IsNotTiff = IsNotHeic = true;
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    private void OnSearchDebounceTick(object? sender, EventArgs e)
    {
        _searchDebounceTimer.Stop();
        ApplyFilter();
    }

    private void OnStoreChanged(StoreChange change)
    {
        System.Windows.Application.Current?.Dispatcher.Invoke(() =>
        {
            ApplyFilter();
        });
    }

    public void RefreshList()
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var allItems = ScreenshotStore.Shared.Items;
        IEnumerable<Screenshot> query = allItems;

        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            string q = SearchQuery.Trim();
            query = query.Where(item => item.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                                        item.Path.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.ToList();
        var currentSelectedPath = SelectedItem?.Path;
        int currentSelectedIndex = SelectedItem != null ? FilteredScreenshots.IndexOf(SelectedItem) : -1;

        FilteredScreenshots.Clear();
        foreach (var item in list)
        {
            FilteredScreenshots.Add(item);
        }

        // Build date groups
        var today = DateTime.Today;
        var groups = list
            .GroupBy(item => item.Created.Date)
            .OrderByDescending(g => g.Key)
            .Select(g => new DateGroup(g.Key, today, g.OrderByDescending(i => i.Created).ToList()))
            .ToList();

        GroupedScreenshots.Clear();
        foreach (var group in groups)
        {
            GroupedScreenshots.Add(group);
        }

        IsEmptyState = FilteredScreenshots.Count == 0;

        // Preserve or update selection to next item
        if (!string.IsNullOrEmpty(currentSelectedPath))
        {
            var match = FilteredScreenshots.FirstOrDefault(x => x.Path.Equals(currentSelectedPath, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                SelectedItem = match;
            }
            else if (FilteredScreenshots.Count > 0 && currentSelectedIndex >= 0)
            {
                int nextIndex = Math.Min(currentSelectedIndex, FilteredScreenshots.Count - 1);
                SelectedItem = FilteredScreenshots[nextIndex];
            }
            else
            {
                SelectedItem = null;
            }
        }
    }

    [RelayCommand]
    private void OpenPreview(Screenshot? item)
    {
        if (item != null)
        {
            OpenPreviewRequested?.Invoke(item);
        }
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedItem != null)
        {
            var itemToDelete = SelectedItem;
            int currentIndex = FilteredScreenshots.IndexOf(itemToDelete);

            DeleteScreenshot(itemToDelete);

            FilteredScreenshots.Remove(itemToDelete);

            if (FilteredScreenshots.Count > 0 && currentIndex >= 0)
            {
                int nextIndex = Math.Min(currentIndex, FilteredScreenshots.Count - 1);
                SelectedItem = FilteredScreenshots[nextIndex];
            }
            else
            {
                SelectedItem = null;
            }
        }
    }

    [RelayCommand]
    private void Undo()
    {
        UndoLastAction();
    }

    [RelayCommand]
    public void CopySelected()
    {
        if (SelectedItem == null || !File.Exists(SelectedItem.Path)) return;
        CopyScreenshotToClipboard(SelectedItem.Path);
    }

    public static void CopyScreenshotToClipboard(string path)
    {
        try
        {
            var dataObj = new System.Windows.DataObject();
            var fileList = new System.Collections.Specialized.StringCollection { path };
            dataObj.SetFileDropList(fileList);

            if (File.Exists(path))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path);
                bitmap.EndInit();
                bitmap.Freeze();
                dataObj.SetImage(bitmap);
            }

            System.Windows.Clipboard.SetDataObject(dataObj, true);
            Log.App.Info($"Copied file to clipboard: {path}");
        }
        catch (Exception ex)
        {
            Log.App.Error($"Failed to copy to clipboard: {ex.Message}");
        }
    }

    [RelayCommand]
    public void PasteClipboard()
    {
        try
        {
            string? targetFolder = ScreenshotStore.Shared.ActiveFolderPath;
            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
            {
                targetFolder = AppSettings.Shared.ActiveFolder;
            }
            if (string.IsNullOrEmpty(targetFolder) || !Directory.Exists(targetFolder))
            {
                Log.App.Warn("Cannot paste: Active folder does not exist.");
                return;
            }

            var pastedPaths = new List<string>();

            if (System.Windows.Clipboard.ContainsFileDropList())
            {
                var dropList = System.Windows.Clipboard.GetFileDropList();

                foreach (string? srcPath in dropList)
                {
                    if (string.IsNullOrEmpty(srcPath) || !File.Exists(srcPath)) continue;
                    if (!ScreenshotFile.IsEligible(srcPath)) continue;

                    string srcDir = Path.GetDirectoryName(srcPath) ?? string.Empty;
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(srcPath);
                    string ext = Path.GetExtension(srcPath);
                    string destPath;

                    if (srcDir.Equals(targetFolder, StringComparison.OrdinalIgnoreCase))
                    {
                        destPath = GenerateUniqueFilePath(targetFolder, $"{fileNameWithoutExt} - 복사본", ext);
                    }
                    else
                    {
                        destPath = GenerateUniqueFilePath(targetFolder, fileNameWithoutExt, ext);
                    }

                    File.Copy(srcPath, destPath, overwrite: false);
                    pastedPaths.Add(destPath);
                    Log.App.Info($"Pasted file from {srcPath} to {destPath}");
                }
            }
            else if (System.Windows.Clipboard.ContainsImage())
            {
                var image = System.Windows.Clipboard.GetImage();
                if (image != null)
                {
                    string baseName = $"스크린샷_{DateTime.Now:yyyy-MM-dd_HHmmss}";
                    string destPath = GenerateUniqueFilePath(targetFolder, baseName, ".png");

                    using (var stream = new FileStream(destPath, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(image));
                        encoder.Save(stream);
                    }

                    pastedPaths.Add(destPath);
                    Log.App.Info($"Pasted clipboard image to {destPath}");
                }
            }

            if (pastedPaths.Count > 0)
            {
                lock (_undoStack)
                {
                    _undoStack.Push(new PasteUndoAction(pastedPaths));
                }
                ScreenshotStore.Shared.TriggerScan();
            }
        }
        catch (Exception ex)
        {
            Log.App.Error($"Failed to paste from clipboard: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task ConvertFormatAsync(string targetExt)
    {
        if (SelectedItem == null || !File.Exists(SelectedItem.Path)) return;

        var item = SelectedItem;
        string srcExt = Path.GetExtension(item.Path).TrimStart('.').ToUpperInvariant();
        string destExt = targetExt.TrimStart('.').ToUpperInvariant();

        ShowStatus($"⏳ {srcExt} → {destExt} 포맷 변환 중...");

        bool success = false;
        await Task.Run(() =>
        {
            success = ConvertImageFormat(item, targetExt);
        });

        if (success)
        {
            ShowStatus($"✅ {destExt} 포맷 변환 완료!", autoHideMs: 2500);
        }
        else
        {
            ShowStatus($"❌ {destExt} 포맷 변환 실패", autoHideMs: 3500);
        }
    }

    public static bool ConvertImageFormat(Screenshot item, string targetExtension)
    {
        if (item == null || string.IsNullOrEmpty(item.Path) || !File.Exists(item.Path)) return false;

        string currentExt = Path.GetExtension(item.Path).ToLowerInvariant();
        string targetExt = targetExtension.StartsWith(".") ? targetExtension.ToLowerInvariant() : "." + targetExtension.ToLowerInvariant();

        if (currentExt == targetExt) return false;

        try
        {
            string dir = Path.GetDirectoryName(item.Path) ?? string.Empty;
            string baseName = Path.GetFileNameWithoutExtension(item.Path);
            string destPath = GenerateUniqueFilePath(dir, baseName, targetExt);

            using (var image = Sukurini.Core.ThumbnailLoader.LoadImageUniversal(item.Path))
            {
                switch (targetExt)
                {
                    case ".png":
                        image.SaveAsPng(destPath);
                        break;
                    case ".jpg":
                    case ".jpeg":
                        image.SaveAsJpeg(destPath);
                        break;
                    case ".webp":
                        image.SaveAsWebp(destPath);
                        break;
                    case ".bmp":
                        image.SaveAsBmp(destPath);
                        break;
                    case ".gif":
                        image.SaveAsGif(destPath);
                        break;
                    case ".tiff":
                    case ".tif":
                        image.SaveAsTiff(destPath);
                        break;
                    default:
                        image.Save(destPath);
                        break;
                }
            }

            // Preserve original file timestamps (CreationTime, LastWriteTime, LastAccessTime)
            try
            {
                File.SetCreationTimeUtc(destPath, File.GetCreationTimeUtc(item.Path));
                File.SetLastWriteTimeUtc(destPath, File.GetLastWriteTimeUtc(item.Path));
                File.SetLastAccessTimeUtc(destPath, File.GetLastAccessTimeUtc(item.Path));
            }
            catch (Exception ex)
            {
                Log.App.Warn($"Failed to copy file timestamps: {ex.Message}");
            }

            Log.App.Info($"Converted image format from {item.Path} to {destPath}");
            lock (_undoStack)
            {
                _undoStack.Push(new PasteUndoAction(new List<string> { destPath }));
            }
            ScreenshotStore.Shared.TriggerScan();
            return true;
        }
        catch (Exception ex)
        {
            Log.App.Error($"Failed format conversion for {item.Path} to {targetExt}: {ex.Message}");
            return false;
        }
    }

    private static string GenerateUniqueFilePath(string folder, string baseName, string extension)
    {
        string candidate = Path.Combine(folder, $"{baseName}{extension}");
        if (!File.Exists(candidate)) return candidate;

        int counter = 2;
        while (true)
        {
            candidate = Path.Combine(folder, $"{baseName} ({counter}){extension}");
            if (!File.Exists(candidate)) return candidate;
            counter++;
        }
    }

    public interface IUndoAction
    {
        Screenshot? PerformUndo();
    }

    public class DeleteUndoAction : IUndoAction
    {
        public string OriginalPath { get; }
        public string TempBackupPath { get; }
        public Screenshot Item { get; }

        public DeleteUndoAction(string originalPath, string tempBackupPath, Screenshot item)
        {
            OriginalPath = originalPath;
            TempBackupPath = tempBackupPath;
            Item = item;
        }

        public Screenshot? PerformUndo()
        {
            try
            {
                if (File.Exists(TempBackupPath))
                {
                    string? dir = Path.GetDirectoryName(OriginalPath);
                    if (!string.IsNullOrEmpty(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.Copy(TempBackupPath, OriginalPath, overwrite: true);
                    try { File.Delete(TempBackupPath); } catch { }

                    Log.App.Info($"Restored deleted file via Undo: {OriginalPath}");
                    return Item;
                }
            }
            catch (Exception ex)
            {
                Log.App.Error($"Failed to undo delete for {OriginalPath}: {ex.Message}");
            }

            return null;
        }
    }

    public class PasteUndoAction : IUndoAction
    {
        public List<string> CreatedPaths { get; }

        public PasteUndoAction(List<string> createdPaths)
        {
            CreatedPaths = createdPaths;
        }

        public Screenshot? PerformUndo()
        {
            foreach (var path in CreatedPaths)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                            path,
                            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                        Log.App.Info($"Undid paste: moved {path} to Recycle Bin");
                    }
                }
                catch (Exception ex)
                {
                    Log.App.Error($"Failed to undo paste for {path}: {ex.Message}");
                }
            }

            return null;
        }
    }

    private static readonly Stack<IUndoAction> _undoStack = new();

    public static bool CanUndo
    {
        get
        {
            lock (_undoStack) { return _undoStack.Count > 0; }
        }
    }

    public static void DeleteScreenshot(Screenshot item)
    {
        try
        {
            if (File.Exists(item.Path))
            {
                // 1. Save temp copy for Ctrl+Z undo
                string tempDir = Path.Combine(Path.GetTempPath(), "Sukurini", "UndoCache");
                Directory.CreateDirectory(tempDir);
                string tempBackupPath = Path.Combine(tempDir, $"{Guid.NewGuid()}{Path.GetExtension(item.Path)}");
                File.Copy(item.Path, tempBackupPath, overwrite: true);

                lock (_undoStack)
                {
                    _undoStack.Push(new DeleteUndoAction(item.Path, tempBackupPath, item));
                }

                // 2. Move ONLY the selected file to Windows Recycle Bin
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    item.Path,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);

                Log.App.Info($"Sent file to Recycle Bin: {item.Path}");
            }
        }
        catch (Exception ex)
        {
            Log.App.Error($"Failed to delete file {item.Path}: {ex.Message}");
        }

        ScreenshotStore.Shared.TriggerScan();
    }

    public static Screenshot? UndoDelete() => UndoLastAction();

    public static Screenshot? UndoLastAction()
    {
        IUndoAction? action = null;
        lock (_undoStack)
        {
            if (_undoStack.Count > 0)
            {
                action = _undoStack.Pop();
            }
        }

        if (action == null) return null;

        var restoredItem = action.PerformUndo();
        ScreenshotStore.Shared.TriggerScan();
        return restoredItem;
    }
}
