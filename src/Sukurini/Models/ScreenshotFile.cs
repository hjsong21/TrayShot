using System;
using System.Collections.Generic;
using System.IO;

namespace Sukurini.Models;

public static class ScreenshotFile
{
    private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".heic", ".webp", ".bmp", ".tiff", ".gif"
    };

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".mov", ".avi", ".mkv"
    };

    public static bool IsEligible(string path)
    {
        var ext = Path.GetExtension(path);
        return ImageExtensions.Contains(ext) || VideoExtensions.Contains(ext);
    }

    public static bool IsImage(string path)
    {
        var ext = Path.GetExtension(path);
        return ImageExtensions.Contains(ext);
    }

    public static bool IsVideo(string path)
    {
        var ext = Path.GetExtension(path);
        return VideoExtensions.Contains(ext);
    }

    public static bool IsConvertible(string path)
    {
        var ext = Path.GetExtension(path);
        return ext.Equals(".png", StringComparison.OrdinalIgnoreCase);
    }

    public static bool NeedsWebpConversion(string pngPath)
    {
        if (!IsConvertible(pngPath)) return false;

        string webpPath = ConvertedPath(pngPath);
        if (File.Exists(webpPath))
        {
            try
            {
                var pngInfo = new FileInfo(pngPath);
                var webpInfo = new FileInfo(webpPath);
                if (pngInfo.Exists && webpInfo.Exists && webpInfo.LastWriteTimeUtc >= pngInfo.LastWriteTimeUtc)
                {
                    return false;
                }
            }
            catch
            {
                return true;
            }
        }
        return true;
    }

    public static string ConvertedPath(string path)
    {
        return Path.ChangeExtension(path, ".webp");
    }
}
