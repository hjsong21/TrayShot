using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        if (string.IsNullOrEmpty(imagePath)) return new DataObject();
        return CreateDragDataObject(new[] { imagePath });
    }

    public static DataObject CreateDragDataObject(IEnumerable<string> imagePaths)
    {
        var dataObj = new DataObject();
        if (imagePaths == null) return dataObj;

        var validPaths = imagePaths.Where(p => !string.IsNullOrEmpty(p) && File.Exists(p)).ToList();
        if (validPaths.Count == 0) return dataObj;

        var targetFilePaths = new List<string>();
        string tempDir = Path.Combine(Path.GetTempPath(), "TrayShotTemp");

        foreach (var path in validPaths)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            string targetPath = path;

            // 1. Generate a temporary PNG file for non-PNG images (e.g. .webp)
            // so that all web/Electron apps (Antigravity 2.0) and chat apps (KakaoTalk) receive a valid PNG file.
            if (ext != ".png")
            {
                try
                {
                    if (!Directory.Exists(tempDir)) Directory.CreateDirectory(tempDir);
                    string tempPngName = $"{Path.GetFileNameWithoutExtension(path)}.png";
                    string tempPngPath = Path.Combine(tempDir, tempPngName);

                    using (var img = ThumbnailLoader.LoadImageUniversal(path))
                    {
                        img.SaveAsPng(tempPngPath);
                    }
                    targetPath = tempPngPath;
                }
                catch (Exception ex)
                {
                    Log.App.Warn($"Failed to create temp PNG for drag-drop ({path}): {ex.Message}");
                }
            }

            targetFilePaths.Add(targetPath);
        }

        if (targetFilePaths.Count == 0) return dataObj;

        // 2. Set FileDrop list with all resolved file paths (Explorer, KakaoTalk multi-image, Office multi-file drop)
        var fileList = new System.Collections.Specialized.StringCollection();
        fileList.AddRange(targetFilePaths.ToArray());
        dataObj.SetFileDropList(fileList);
        dataObj.SetData(DataFormats.FileDrop, targetFilePaths.ToArray());

        // 3. If single file, also provide DIB and Bitmap for direct paste compatibility (Word/HWP/Paint single image drop)
        if (targetFilePaths.Count == 1)
        {
            string singleFile = targetFilePaths[0];

            // Set DeviceIndependentBitmap (CF_DIB) stream for MS Office (Word/PPT), HWP (한글), and Paint (그림판)
            try
            {
                using var img = ThumbnailLoader.LoadImageUniversal(singleFile);
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

            // Set WPF BitmapSource & SetImage for WPF/Standard apps
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(singleFile);
                bitmap.EndInit();
                bitmap.Freeze();

                dataObj.SetImage(bitmap);
                dataObj.SetData(DataFormats.Bitmap, bitmap);
            }
            catch (Exception bmpEx)
            {
                Log.App.Warn($"Failed to set Bitmap for drag-drop: {bmpEx.Message}");
            }
        }

        return dataObj;
    }
}
