using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using TrayShot.Gallery;
using TrayShot.Infrastructure;
using TrayShot.Models;
using Xunit;

namespace TrayShot.Tests.Gallery;

public class GalleryViewModelTests
{
    [Fact]
    public void GalleryViewModel_InitializesAndHandlesFilter()
    {
        var vm = new GalleryViewModel();
        Assert.NotNull(vm.FilteredScreenshots);

        vm.SearchQuery = "NonExistentSearchTerm_12345";
        // Filter application check
        vm.RefreshList();
        Assert.True(vm.IsEmptyState || vm.FilteredScreenshots.Count == 0);
    }

    [Fact]
    public void GalleryViewModel_MultiSelection_ToggleAndClear()
    {
        var vm = new GalleryViewModel();
        var item1 = new Screenshot(@"C:\mock\shot1.png", DateTime.Now, 1000);
        var item2 = new Screenshot(@"C:\mock\shot2.png", DateTime.Now, 2000);
        var item3 = new Screenshot(@"C:\mock\shot3.png", DateTime.Now, 3000);

        // Single selection
        vm.SetSingleSelection(item1);
        Assert.True(vm.IsSelected(item1));
        Assert.False(vm.IsSelected(item2));
        Assert.Single(vm.SelectedItems);

        // Toggle add item2
        vm.ToggleSelection(item2);
        Assert.True(vm.IsSelected(item1));
        Assert.True(vm.IsSelected(item2));
        Assert.Equal(2, vm.SelectedItems.Count);
        Assert.Equal(item2, vm.SelectedItem);

        // Toggle remove item1
        vm.ToggleSelection(item1);
        Assert.False(vm.IsSelected(item1));
        Assert.True(vm.IsSelected(item2));
        Assert.Single(vm.SelectedItems);
        Assert.Equal(item2, vm.SelectedItem);

        // Clear
        vm.ClearSelection();
        Assert.Empty(vm.SelectedItems);
        Assert.Null(vm.SelectedItem);
    }

    [Fact]
    public void DragDropHelper_MultiFilePayload_CreatesValidFileDrop()
    {
        string tempDir = Path.Combine(Path.GetTempPath(), $"trayshot_dd_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            string file1 = Path.Combine(tempDir, "shot1.png");
            string file2 = Path.Combine(tempDir, "shot2.png");
            File.WriteAllText(file1, "dummy data 1");
            File.WriteAllText(file2, "dummy data 2");

            var dataObj = DragDropHelper.CreateDragDataObject(new[] { file1, file2 });
            Assert.NotNull(dataObj);

            var fileDrop = dataObj.GetFileDropList();
            Assert.Equal(2, fileDrop.Count);
            Assert.Contains(file1, fileDrop.Cast<string>());
            Assert.Contains(file2, fileDrop.Cast<string>());
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                try { Directory.Delete(tempDir, true); } catch { }
            }
        }
    }
}
