using System;
using System.IO;
using Sukurini.Infrastructure;

namespace Sukurini.Organize;

public sealed class ScreenshotOrganizer
{
    public static string MoveToDateFolder(string filePath, string targetBaseFolder, string pattern)
    {
        if (!File.Exists(filePath) || !Directory.Exists(targetBaseFolder))
            return filePath;

        DateTime creationDate = File.GetCreationTimeUtc(filePath);
        string subFolder = DateFolderFormat.FormatDate(creationDate, pattern);
        string destFolder = Path.Combine(targetBaseFolder, subFolder);

        if (!Directory.Exists(destFolder))
        {
            Directory.CreateDirectory(destFolder);
        }

        string fileName = Path.GetFileName(filePath);
        string destPath = Path.Combine(destFolder, fileName);

        if (filePath.Equals(destPath, StringComparison.OrdinalIgnoreCase))
            return filePath;

        destPath = ResolveNameCollision(destPath);

        File.Move(filePath, destPath);
        Log.Organize.Info($"Moved file from {filePath} to {destPath}");
        return destPath;
    }

    private static string ResolveNameCollision(string targetPath)
    {
        if (!File.Exists(targetPath)) return targetPath;

        string folder = Path.GetDirectoryName(targetPath)!;
        string nameWithoutExt = Path.GetFileNameWithoutExtension(targetPath);
        string ext = Path.GetExtension(targetPath);

        for (int i = 2; i <= 99; i++)
        {
            string candidate = Path.Combine(folder, $"{nameWithoutExt} {i}{ext}");
            if (!File.Exists(candidate)) return candidate;
        }

        return Path.Combine(folder, $"{nameWithoutExt}_{Guid.NewGuid():N}{ext}");
    }
}
