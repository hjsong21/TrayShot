using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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
    private bool _isEmptyState;

    private readonly DispatcherTimer _searchDebounceTimer;

    public ObservableCollection<Screenshot> FilteredScreenshots { get; } = new();

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
        FilteredScreenshots.Clear();
        foreach (var item in list)
        {
            FilteredScreenshots.Add(item);
        }

        IsEmptyState = FilteredScreenshots.Count == 0;
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
            DeleteScreenshot(SelectedItem);
        }
    }

    [RelayCommand]
    private void Undo()
    {
        UndoDelete();
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

            if (System.Windows.Clipboard.ContainsFileDropList())
            {
                var dropList = System.Windows.Clipboard.GetFileDropList();
                bool pastedAny = false;

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
                    pastedAny = true;
                    Log.App.Info($"Pasted file from {srcPath} to {destPath}");
                }

                if (pastedAny)
                {
                    ScreenshotStore.Shared.TriggerScan();
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

                    Log.App.Info($"Pasted clipboard image to {destPath}");
                    ScreenshotStore.Shared.TriggerScan();
                }
            }
        }
        catch (Exception ex)
        {
            Log.App.Error($"Failed to paste from clipboard: {ex.Message}");
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

    public record DeletedItemBackup(string OriginalPath, string TempBackupPath, Screenshot Item);

    private static readonly Stack<DeletedItemBackup> _undoStack = new();

    public static bool CanUndo => _undoStack.Count > 0;

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

                _undoStack.Push(new DeletedItemBackup(item.Path, tempBackupPath, item));

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

    public static Screenshot? UndoDelete()
    {
        if (_undoStack.Count == 0) return null;

        var backup = _undoStack.Pop();
        try
        {
            if (File.Exists(backup.TempBackupPath))
            {
                string? dir = Path.GetDirectoryName(backup.OriginalPath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.Copy(backup.TempBackupPath, backup.OriginalPath, overwrite: true);
                try { File.Delete(backup.TempBackupPath); } catch { }

                Log.App.Info($"Restored file via Undo: {backup.OriginalPath}");
                ScreenshotStore.Shared.TriggerScan();
                return backup.Item;
            }
        }
        catch (Exception ex)
        {
            Log.App.Error($"Failed to undo delete for {backup.OriginalPath}: {ex.Message}");
        }

        return null;
    }
}
