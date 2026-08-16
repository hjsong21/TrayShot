using System.Threading.Tasks;
using TrayShot.Gallery;
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
}
