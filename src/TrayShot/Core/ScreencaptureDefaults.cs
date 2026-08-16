using System;
using System.IO;
using Microsoft.Win32;
using TrayShot.Infrastructure;

namespace TrayShot.Core;

public static class ScreencaptureDefaults
{
    public static string GetCurrentLocation()
    {
        try
        {
            // Windows Registry: Shell Folders - Screenshots
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", false);
            if (key?.GetValue("{7D83EE9B-2244-4E70-B1F5-9C016B05CE8E}") is string regPath && !string.IsNullOrEmpty(regPath))
            {
                string expanded = Environment.ExpandEnvironmentVariables(regPath);
                if (Directory.Exists(expanded)) return expanded;
            }
        }
        catch (Exception ex)
        {
            Log.App.Debug($"Failed to read registry screenshot location: {ex.Message}");
        }

        // Fallback: Pictures/Screenshots
        string picturesDir = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string screenshotsDir = Path.Combine(picturesDir, "Screenshots");
        if (Directory.Exists(screenshotsDir)) return screenshotsDir;

        // Ultimate fallback: Desktop
        return Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    }
}
