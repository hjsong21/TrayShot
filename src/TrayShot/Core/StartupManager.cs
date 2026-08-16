using System;
using System.IO;
using Microsoft.Win32;
using TrayShot.Infrastructure;

namespace TrayShot.Core;

public static class StartupManager
{
    private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "TrayShot";

    public static bool IsStartupEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
            return key?.GetValue(AppName) != null;
        }
        catch
        {
            return false;
        }
    }

    public static void SetStartupEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
            if (key == null) return;

            if (enabled)
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                if (!string.IsNullOrEmpty(exePath))
                {
                    key.SetValue(AppName, $"\"{exePath}\"");
                    Log.Settings.Info("Enabled launch at startup");
                }
            }
            else
            {
                key.DeleteValue(AppName, false);
                Log.Settings.Info("Disabled launch at startup");
            }
        }
        catch (Exception ex)
        {
            Log.Settings.Error($"Failed to update startup registry: {ex.Message}");
        }
    }
}
