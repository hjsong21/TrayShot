using System;
using System.IO;
using Sukurini.Organize;
using Xunit;

namespace Sukurini.Tests.Organize;

public class ScreenshotOrganizerTests : IDisposable
{
    private readonly string _tempDirPath;

    public ScreenshotOrganizerTests()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), $"sukurini_org_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirPath);
    }

    [Fact]
    public void ScreenshotOrganizer_MovesFileToDateFolderWithCollisionResolution()
    {
        string sampleFile = Path.Combine(_tempDirPath, "shot.png");
        File.WriteAllText(sampleFile, "mock content");

        string movedPath = ScreenshotOrganizer.MoveToDateFolder(sampleFile, _tempDirPath, "yyyy/MM");

        Assert.True(File.Exists(movedPath));
        Assert.False(File.Exists(sampleFile));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirPath))
        {
            try { Directory.Delete(_tempDirPath, true); } catch { }
        }
    }
}
