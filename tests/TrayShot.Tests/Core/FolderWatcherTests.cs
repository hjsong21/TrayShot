using System;
using System.IO;
using System.Threading;
using TrayShot.Core;
using Xunit;

namespace TrayShot.Tests.Core;

public class FolderWatcherTests : IDisposable
{
    private readonly string _tempDirPath;

    public FolderWatcherTests()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), $"trayshot_watcher_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirPath);
    }

    [Fact]
    public void FolderWatcher_FiresEventOnNewFileCreated()
    {
        var resetEvent = new ManualResetEventSlim(false);

        using var watcher = new FolderWatcher(
            _tempDirPath,
            TimeSpan.FromMilliseconds(100),
            onChange: () => resetEvent.Set(),
            onFolderLost: () => { });

        watcher.Start();

        string newFile = Path.Combine(_tempDirPath, "test_image.png");
        File.WriteAllText(newFile, "dummy content");

        bool eventFired = resetEvent.Wait(2000);
        Assert.True(eventFired);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirPath))
        {
            try { Directory.Delete(_tempDirPath, true); } catch { }
        }
    }
}
