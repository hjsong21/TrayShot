using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TrayShot.Core;
using TrayShot.Models;
using Xunit;

namespace TrayShot.Tests.Core;

public class ScreenshotStoreTests : IDisposable
{
    private readonly string _tempDirPath;

    public ScreenshotStoreTests()
    {
        _tempDirPath = Path.Combine(Path.GetTempPath(), $"trayshot_store_{Guid.NewGuid():N}");
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

    [Fact]
    public async Task ScreenshotStore_TriggerScanWithReplacement_PlacesWebpInReplacementsNotInserted()
    {
        using var store = new ScreenshotStore();
        store.Initialize(_tempDirPath, includeSubfolders: false);

        string testPng = Path.Combine(_tempDirPath, "capture.png");
        File.WriteAllText(testPng, "png binary payload mock");

        var firstScanEvent = new ManualResetEventSlim(false);
        store.Changed += change => firstScanEvent.Set();
        store.TriggerScan();
        firstScanEvent.Wait(3000);

        // Now simulate WebP creation and PNG removal (replacement)
        string testWebp = Path.Combine(_tempDirPath, "capture.webp");
        File.WriteAllText(testWebp, "webp binary payload mock");
        File.Delete(testPng);

        var replaceEvent = new ManualResetEventSlim(false);
        StoreChange? replaceChange = null;

        store.Changed += change =>
        {
            if (change.Replacements.Count > 0)
            {
                replaceChange = change;
                replaceEvent.Set();
            }
        };

        store.TriggerScanWithReplacement(testPng, testWebp);

        bool fired = replaceEvent.Wait(3000);
        Assert.True(fired);
        Assert.NotNull(replaceChange);

        // WebP should be present in Replacements mapping
        Assert.True(replaceChange.Replacements.ContainsKey(testPng));
        Assert.Equal(testWebp, replaceChange.Replacements[testPng]);

        // WebP must NOT be in Inserted list
        Assert.DoesNotContain(replaceChange.Inserted, item => item.Path.Equals(testWebp, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirPath))
        {
            try { Directory.Delete(_tempDirPath, true); } catch { }
        }
    }
}
