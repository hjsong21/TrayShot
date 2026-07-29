using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        _searchDebounceTimer.Stop();
        _searchDebounceTimer.Start();
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
}
