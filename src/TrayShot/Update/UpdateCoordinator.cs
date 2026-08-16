using System;
using System.Threading.Tasks;
using TrayShot.Infrastructure;

namespace TrayShot.Update;

public sealed class UpdateCoordinator
{
    public static async Task CheckForUpdatesAsync()
    {
        try
        {
            Log.Update.Info($"Checking for updates on channel={AppSettings.Shared.UpdateChannel}");
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            Log.Update.Error($"Update check error: {ex.Message}");
        }
    }
}
