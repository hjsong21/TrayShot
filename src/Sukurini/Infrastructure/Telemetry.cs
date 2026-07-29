using System;
using System.Collections.Generic;

namespace Sukurini.Infrastructure;

public enum TelemetryEvent
{
    AppLaunch,
    SettingChanged,
    ScreenshotCaptured,
    OcrProcessed,
    WebpConverted
}

public static class Telemetry
{
    public static void LogEvent(TelemetryEvent eventType, Dictionary<string, string>? data = null)
    {
        if (!AppSettings.Shared.AnalyticsEnabled)
            return;

        Log.Telemetry.Info($"Telemetry event={eventType} dataCount={data?.Count ?? 0}");
    }
}
