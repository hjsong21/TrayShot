using System;
using System.IO;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TrayShot.Core;
using Xunit;

namespace TrayShot.Tests.Core;

public class ThumbnailLoaderTests : IDisposable
{
    private readonly string _tempImagePath;

    public ThumbnailLoaderTests()
    {
        _tempImagePath = Path.Combine(Path.GetTempPath(), $"trayshot_test_img_{Guid.NewGuid():N}.png");

        using var image = new Image<Rgba32>(800, 600);
        image.SaveAsPng(_tempImagePath);
    }

    [Fact]
    public async Task ThumbnailLoader_LoadsThumbnailAndCachesResult()
    {
        var loader = ThumbnailLoader.Shared;
        loader.ClearCache();

        var bitmap = await loader.LoadThumbnailAsync(_tempImagePath, maxPixelSize: 200);

        Assert.NotNull(bitmap);
        Assert.Equal(1, loader.CacheCount);

        // 캐시 재사용 테스트
        var cachedBitmap = await loader.LoadThumbnailAsync(_tempImagePath, maxPixelSize: 200);
        Assert.Same(bitmap, cachedBitmap);
    }

    public void Dispose()
    {
        if (File.Exists(_tempImagePath))
        {
            try { File.Delete(_tempImagePath); } catch { }
        }
    }
}
