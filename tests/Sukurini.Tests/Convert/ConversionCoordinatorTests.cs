using System;
using System.IO;
using System.Threading;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Sukurini.Convert;
using Sukurini.Core;
using Sukurini.Infrastructure;
using Xunit;

namespace Sukurini.Tests.Convert;

public class ConversionCoordinatorTests : IDisposable
{
    private readonly string _tempDirPath;

    public ConversionCoordinatorTests()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), $"sukurini_conv_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirPath);
    }

    [Fact]
    public void ConversionCoordinator_ConvertsPngToWebpWhenEnabled()
    {
        AppSettings.Shared.WebpConversionEnabled = true;
        AppSettings.Shared.WebpDisposal = WebPDisposal.Keep;

        string pngPath = Path.Combine(_tempDirPath, "test_shot.png");
        using (var img = new Image<Rgba32>(50, 50))
        {
            img.SaveAsPng(pngPath);
        }

        var coordinator = ConversionCoordinator.Shared;
        coordinator.Initialize();
        ScreenshotStore.Shared.Initialize(_tempDirPath, false);

        coordinator.Enqueue(pngPath, ConversionOrigin.Live);

        // Wait for async conversion worker
        Thread.Sleep(1500);

        string expectedWebp = Path.Combine(_tempDirPath, "test_shot.webp");
        Assert.True(File.Exists(expectedWebp));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirPath))
        {
            try { Directory.Delete(_tempDirPath, true); } catch { }
        }
    }
}
