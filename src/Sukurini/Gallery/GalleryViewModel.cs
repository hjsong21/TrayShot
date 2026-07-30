using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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
