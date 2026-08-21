using System;
using System.IO;
using System.Threading;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TrayShot.Convert;
using TrayShot.Core;
using TrayShot.Infrastructure;
using Xunit;

namespace TrayShot.Tests.Convert;

[Collection("Non-Parallel-Settings")]
public class ConversionCoordinatorTests : IDisposable
{
    private readonly string _tempDirPath;
    private readonly TestSettingsScope _settingsScope;

    public ConversionCoordinatorTests()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), $"trayshot_conv_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirPath);

        _settingsScope = new TestSettingsScope();
    }

    [Fact]
    public void ConversionCoordinator_ConvertsPngToWebpWhenEnabled()
    {
        AppSettings.Shared.WebpConversionEnabled = true;
        AppSettings.Shared.WebpDisposal = WebPDisposal.Keep;

        string pngPath = Path.Combine(_tempDirPath, "test_shot.png");
        using (var img = new Image<Rgba32>(200, 200))
        {
            img.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        row[x] = new Rgba32((byte)(x % 255), (byte)(y % 255), 150, 255);
                    }
                }
            });
            img.SaveAsPng(pngPath);
        }

        var coordinator = ConversionCoordinator.Shared;
        coordinator.Initialize();
        ScreenshotStore.Shared.Initialize(_tempDirPath, false);

        coordinator.Enqueue(pngPath, ConversionOrigin.Live);

        // Wait for async conversion worker
        string expectedWebp = Path.Combine(_tempDirPath, "test_shot.webp");
        bool exists = false;
        for (int i = 0; i < 50; i++)
        {
            if (File.Exists(expectedWebp))
            {
                exists = true;
                break;
            }
            Thread.Sleep(100);
        }

        Assert.True(exists);
    }

    public void Dispose()
    {
        _settingsScope.Dispose();
        if (Directory.Exists(_tempDirPath))
        {
            try { Directory.Delete(_tempDirPath, true); } catch { }
        }
    }
}
