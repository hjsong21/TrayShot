using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace TrayShot.Models;

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

    private static readonly Regex DateRegex = new(
        @"\b(20\d{2})[-_.]?(0[1-9]|1[0-2])[-_.]?(0[1-9]|[12]\d|3[01])\b",
        RegexOptions.Compiled);

    /// <summary>
    /// 파일명 및 파일 타임스탬프(CreationTime, LastWriteTime)를 기반으로 스크린샷의 실제 작성/촬영 로컬 시각을 반환합니다.
    /// </summary>
    public static DateTime GetEffectiveCreatedTime(FileInfo fileInfo)
    {
        // 1. 파일명에서 yyyy-MM-dd 또는 yyyyMMdd 날짜 패턴 검색 시도
        try
        {
            var match = DateRegex.Match(fileInfo.Name);
            if (match.Success)
            {
                int year = int.Parse(match.Groups[1].Value);
                int month = int.Parse(match.Groups[2].Value);
                int day = int.Parse(match.Groups[3].Value);

                if (year >= 2000 && year <= 2100 && month >= 1 && month <= 12 && day >= 1 && day <= DateTime.DaysInMonth(year, month))
                {
                    // 파일 수정 시각의 시/분/초 유지하면서 날짜만 파일명 날짜로 지정
                    var time = fileInfo.LastWriteTime;
                    return new DateTime(year, month, day, time.Hour, time.Minute, time.Second, DateTimeKind.Local);
                }
            }
        }
        catch
        {
            // Fallback to file timestamps
        }

        // 2. Windows 파일 복사 특성상 CreationTime은 복사 시점으로 갱신되고 LastWriteTime에 원본 시각이 보존됩니다.
        //    CreationTime과 LastWriteTime 중 더 이전(과거) 시각을 로컬 타임으로 채택합니다.
        var createdLocal = fileInfo.CreationTime;
        var modifiedLocal = fileInfo.LastWriteTime;

        return createdLocal < modifiedLocal ? createdLocal : modifiedLocal;
    }
}
