using System;
using System.IO;

namespace TrayShot.Organize;

public static class DateFolderFormat
{
    public const string DefaultPattern = "yyyy/MM";
    public const int MaximumDepth = 4;

    public static string FormatDate(DateTime date, string pattern)
    {
        try
        {
            return date.ToString(string.IsNullOrWhiteSpace(pattern) ? DefaultPattern : pattern);
        }
        catch
        {
            return date.ToString(DefaultPattern);
        }
    }
}
