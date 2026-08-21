using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using TrayShot.Core;

namespace TrayShot.Infrastructure;

/// <summary>
/// Helper class for creating universal multi-format drag and drop DataObject payloads
/// compatible with MS Office (Word/PowerPoint/Excel), HWP (Hancom Hangul), Windows Paint,
/// web editors (Antigravity 2.0 / Electron), and messenger apps (KakaoTalk).
/// </summary>
public static class DragDropHelper
{
    public static DataObject CreateDragDataObject(string imagePath)
    {
        var dataObj = new DataObject();
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) return dataObj;

        string ext = Path.GetExtension(imagePath).ToLowerInvariant();

        // 1. Generate a single temporary PNG file for non-PNG images (e.g. .webp)
        // so that all web/Electron apps (Antigravity 2.0) and chat apps (KakaoTalk) receive a single valid PNG file.
        string targetFilePath = imagePath;
        if (ext != ".png")
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "TrayShotTemp");
                if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                string tempPngName = $"{Path.GetFileNameWithoutExtension(imagePath)}.png";
                string tempPngPath = Path.Combine(tempDir, tempPngName);

                using (var img = ThumbnailLoader.LoadImageUniversal(imagePath))
                {
                    img.SaveAsPng(tempPngPath);
                }
                targetFilePath = tempPngPath;
            }
            catch (Exception ex)
            {
                Log.App.Warn($"Failed to create temp PNG for drag-drop: {ex.Message}");
            }
        }

        // 2. Set FileDrop list with EXACTLY ONE single file path to prevent duplicate item insertion
        var fileList = new System.Collections.Specialized.StringCollection { targetFilePath };
        dataObj.SetFileDropList(fileList);
        dataObj.SetData(DataFormats.FileDrop, new string[] { targetFilePath });

        // 3. Set DeviceIndependentBitmap (CF_DIB) stream for MS Office (Word/PPT), HWP (한글), and Paint (그림판)
        try
        {
            using var img = ThumbnailLoader.LoadImageUniversal(targetFilePath);
            using var bmpMs = new MemoryStream();
            img.SaveAsBmp(bmpMs);
            byte[] bmpBytes = bmpMs.ToArray();

            // BMP file header is 14 bytes (0x00..0x0D).
            // CF_DIB is the BMP payload starting immediately after the 14-byte BITMAPFILEHEADER.
            if (bmpBytes.Length > 14)
            {
                byte[] dibBytes = new byte[bmpBytes.Length - 14];
                Array.Copy(bmpBytes, 14, dibBytes, 0, dibBytes.Length);
                var dibStream = new MemoryStream(dibBytes);
                dataObj.SetData("DeviceIndependentBitmap", dibStream);
            }
        }
        catch (Exception dibEx)
        {
            Log.App.Warn($"Failed to set DeviceIndependentBitmap for drag-drop: {dibEx.Message}");
        }

        // 4. Set WPF BitmapSource & SetImage for WPF/Standard apps
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(targetFilePath);
            bitmap.EndInit();
            bitmap.Freeze();

            dataObj.SetImage(bitmap);
            dataObj.SetData(DataFormats.Bitmap, bitmap);
        }
        catch (Exception bmpEx)
        {
            Log.App.Warn($"Failed to set Bitmap for drag-drop: {bmpEx.Message}");
        }

        return dataObj;
    }
}
