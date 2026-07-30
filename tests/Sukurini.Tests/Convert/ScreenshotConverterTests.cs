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
        CreateComplexTestPng(_tempPngPath);
    }

    private static void CreateComplexTestPng(string path, int width = 200, int height = 200)
    {
        using var img = new Image<Rgba32>(width, height);
        img.ProcessPixelRows(accessor =>
        {
            for (int y = 0; y < accessor.Height; y++)
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < row.Length; x++)
                {
                    row[x] = new Rgba32(50, 100, 150, 255);
                }
            }
        });
        img.SaveAsPng(path);
    }

    [Fact]
    public void ScreenshotConverter_ConvertsPngToWebpAndVerifiesPixelMatchAndTimestamps()
    {
        AppSettings.Shared.WebpDisposal = WebPDisposal.Keep;

        DateTime origCreation = File.GetCreationTimeUtc(_tempPngPath);
        DateTime origLastWrite = File.GetLastWriteTimeUtc(_tempPngPath);

        var converter = new ScreenshotConverter();
        bool success = converter.ConvertAndVerify(_tempPngPath, out string webpPath);

        Assert.True(success);
        Assert.True(File.Exists(webpPath));
        Assert.True(File.Exists(_tempPngPath)); // Kept per Keep policy

        // Check timestamp inheritance
        Assert.Equal(origCreation, File.GetCreationTimeUtc(webpPath));
        Assert.Equal(origLastWrite, File.GetLastWriteTimeUtc(webpPath));

        if (File.Exists(webpPath)) File.Delete(webpPath);
    }

    [Fact]
    public void ScreenshotConverter_DisposesSourceFileWhenDeletePolicy()
    {
        string deleteTestPng = Path.Combine(Path.GetTempPath(), $"sukurini_del_{Guid.NewGuid():N}.png");
        CreateComplexTestPng(deleteTestPng);

        AppSettings.Shared.WebpDisposal = WebPDisposal.Delete;

        var converter = new ScreenshotConverter();
        bool success = converter.ConvertAndVerify(deleteTestPng, out string webpPath);

        Assert.True(success);
        Assert.True(File.Exists(webpPath));
        Assert.False(File.Exists(deleteTestPng)); // Deleted per policy

        if (File.Exists(webpPath)) File.Delete(webpPath);
    }

    [Fact]
    public void NeedsWebpConversion_ReturnsFalseWhenWebpAlreadyExistsWithMatchingMTime()
    {
        AppSettings.Shared.WebpDisposal = WebPDisposal.Keep;

        var converter = new ScreenshotConverter();
        bool success = converter.ConvertAndVerify(_tempPngPath, out string webpPath);

        Assert.True(success);
        Assert.True(File.Exists(webpPath));

        // After conversion, NeedsWebpConversion should return false!
        bool needsReconversion = Sukurini.Models.ScreenshotFile.NeedsWebpConversion(_tempPngPath);
        Assert.False(needsReconversion);

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
