using Sukurini.Models;
using Xunit;

namespace Sukurini.Tests.Models;

public class ScreenshotFileTests
{
    [Theory]
    [InlineData("test.png", true)]
    [InlineData("test.JPG", true)]
    [InlineData("video.mp4", true)]
    [InlineData("document.pdf", false)]
    [InlineData("script.sh", false)]
    public void IsEligible_ValidatesExtensionsCorrectly(string filename, bool expected)
    {
        Assert.Equal(expected, ScreenshotFile.IsEligible(filename));
    }

    [Theory]
    [InlineData("test.png", true)]
    [InlineData("test.jpg", false)]
    [InlineData("video.mp4", false)]
    public void IsConvertible_IdentifiesPngOnly(string filename, bool expected)
    {
        Assert.Equal(expected, ScreenshotFile.IsConvertible(filename));
    }

    [Fact]
    public void ConvertedPath_ChangesExtensionToWebp()
    {
        string original = @"C:\Pictures\image.png";
        string converted = ScreenshotFile.ConvertedPath(original);
        Assert.Equal(@"C:\Pictures\image.webp", converted);
    }
}
