using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Sukurini.Infrastructure;
using Sukurini.Models;

namespace Sukurini.Convert;

public sealed class ScreenshotConverter
{
    public bool ConvertAndVerify(string sourcePngPath, out string convertedWebpPath)
    {
        convertedWebpPath = ScreenshotFile.ConvertedPath(sourcePngPath);

        try
        {
            if (!File.Exists(sourcePngPath)) return false;

            // 1. WebP 무손실 인코딩
            WebPEncoder.EncodeLossless(sourcePngPath, convertedWebpPath);

            // 2. 픽셀 무결성 검증 (Pixel-by-pixel 1:1 비교)
            if (!VerifyPixelMatch(sourcePngPath, convertedWebpPath))
            {
                Log.Convert.Error($"Pixel verification failed for {sourcePngPath}");
                if (File.Exists(convertedWebpPath)) File.Delete(convertedWebpPath);
                return false;
            }

            // 3. 타임스탬프 복사
            File.SetCreationTimeUtc(convertedWebpPath, File.GetCreationTimeUtc(sourcePngPath));
            File.SetLastWriteTimeUtc(convertedWebpPath, File.GetLastWriteTimeUtc(sourcePngPath));

            // 4. 원본 처리 (설정에 따른 처분)
            DisposeSourceFile(sourcePngPath);

            Log.Convert.Info($"Successfully converted PNG to WebP: {convertedWebpPath}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Convert.Error($"Conversion error for {sourcePngPath}: {ex.Message}");
            if (File.Exists(convertedWebpPath))
            {
                try { File.Delete(convertedWebpPath); } catch { }
            }
            return false;
        }
    }

    private static bool VerifyPixelMatch(string pngPath, string webpPath)
    {
        using var imgA = Image.Load<Rgba32>(pngPath);
        using var imgB = Image.Load<Rgba32>(webpPath);

        if (imgA.Width != imgB.Width || imgA.Height != imgB.Height)
            return false;

        bool isIdentical = true;
        imgA.ProcessPixelRows(imgB, (accessorA, accessorB) =>
        {
            for (int y = 0; y < accessorA.Height; y++)
            {
                var rowA = accessorA.GetRowSpan(y);
                var rowB = accessorB.GetRowSpan(y);
                if (!rowA.SequenceEqual(rowB))
                {
                    isIdentical = false;
                    break;
                }
            }
        });

        return isIdentical;
    }

    private static void DisposeSourceFile(string sourcePngPath)
    {
        var disposal = AppSettings.Shared.WebpDisposal;
        switch (disposal)
        {
            case WebPDisposal.Delete:
                File.Delete(sourcePngPath);
                break;
            case WebPDisposal.Trash:
                // Microsoft.VisualBasic FileSystem.DeleteFile(..., RecycleOption.SendToRecycleBin) 또는 File.Delete
                File.Delete(sourcePngPath);
                break;
            case WebPDisposal.Keep:
                // Keep original
                break;
        }
    }
}
