using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sukurini.Core;
using Sukurini.Models;
using Xunit;

namespace Sukurini.Tests.Core;

public class ScreenshotStoreTests : IDisposable
{
    private readonly string _tempDirPath;

    public ScreenshotStoreTests()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), $"sukurini_store_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirPath);
    }

    [Fact]
    public async Task ScreenshotStore_ScansAndFiresChangedEventOnNewFile()
    {
        using var store = new ScreenshotStore();
        var resetEvent = new ManualResetEventSlim(false);
        StoreChange? capturedChange = null;

        store.Changed += change =>
        {
            capturedChange = change;
            resetEvent.Set();
        };

        store.Initialize(_tempDirPath, includeSubfolders: false);

        string testImage = Path.Combine(_tempDirPath, "shot1.png");
        File.WriteAllText(testImage, "png binary payload mock");

        store.TriggerScan();

        bool fired = resetEvent.Wait(3000);
        Assert.True(fired);
        Assert.NotNull(capturedChange);
        Assert.NotEmpty(store.Items);
        Assert.Equal(testImage, store.Items[0].Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirPath))
        {
            try { Directory.Delete(_tempDirPath, true); } catch { }
        }
    }
}
