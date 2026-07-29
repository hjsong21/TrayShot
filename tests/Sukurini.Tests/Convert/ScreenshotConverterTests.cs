using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Sukurini.Convert;
using Sukurini.Infrastructure;
using Xunit;

namespace Sukurini.Tests.Convert;

public class ScreenshotConverterTests : IDisposable
{
    private readonly string _tempPngPath;

    public ScreenshotConverterTests()
    {
        _tempPngPath = Path.Combine(Path.GetTempPath(), $"sukurini_test_{Guid.NewGuid():N}.png");

        using var img = new Image<Rgba32>(100, 100);
        img.SaveAsPng(_tempPngPath);
    }

    [Fact]
    public void ScreenshotConverter_ConvertsPngToWebpAndVerifiesPixelMatch()
    {
        AppSettings.Shared.WebpDisposal = WebPDisposal.Keep;

        var converter = new ScreenshotConverter();
        bool success = converter.ConvertAndVerify(_tempPngPath, out string webpPath);

        Assert.True(success);
        Assert.True(File.Exists(webpPath));

        if (File.Exists(webpPath)) File.Delete(webpPath);
    }

    public void Dispose()
    {
        if (File.Exists(_tempPngPath))
        {
            try { File.Delete(_tempPngPath); } catch { }
        }
    }
}
