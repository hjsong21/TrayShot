using System;
using System.IO;
using SixLabors.ImageSharp;

namespace Sukurini.Convert;

public static class PngExporter
{
    public static string GetOrExportPng(string imagePath)
    {
        if (Path.GetExtension(imagePath).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return imagePath;
        }

        string tempDir = Path.Combine(Path.GetTempPath(), "Sukurini-CopyAsPNG");
        if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);

        string tempPngPath = Path.Combine(tempDir, $"{Path.GetFileNameWithoutExtension(imagePath)}.png");

        if (!File.Exists(tempPngPath))
        {
            using var image = Image.Load(imagePath);
            image.SaveAsPng(tempPngPath);
        }

        return tempPngPath;
    }
}
