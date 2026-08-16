using System;
using System.IO;
using TrayShot.Core;
using TrayShot.Infrastructure;

namespace TrayShot.Organize;

public sealed class OrganizeCoordinator
{
    public void OrganizeFolder(string folderPath)
    {
        if (!AppSettings.Shared.OrganizeEnabled || !Directory.Exists(folderPath))
            return;

        string pattern = AppSettings.Shared.OrganizeFormat;
        var files = Directory.GetFiles(folderPath);

        foreach (var file in files)
        {
            try
            {
                ScreenshotOrganizer.MoveToDateFolder(file, folderPath, pattern);
            }
            catch (Exception ex)
            {
                Log.Organize.Error($"Failed to organize file {file}: {ex.Message}");
            }
        }

        ScreenshotStore.Shared.TriggerScan();
    }
}
