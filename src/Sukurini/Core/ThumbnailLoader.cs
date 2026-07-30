using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Sukurini.Infrastructure;

namespace Sukurini.Core;

public sealed class ThumbnailLoader
{
    private static readonly Lazy<ThumbnailLoader> _instance = new(() => new ThumbnailLoader());
    public static ThumbnailLoader Shared => _instance.Value;

    private readonly ConcurrentDictionary<string, BitmapSource> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Task<BitmapSource?>> _pendingTasks = new(StringComparer.OrdinalIgnoreCase);

    public int CacheCount => _cache.Count;

    public async Task<BitmapSource?> LoadThumbnailAsync(string imagePath, int maxPixelSize = 360)
    {
        if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
            return null;

        string cacheKey = $"{imagePath}_{maxPixelSize}";
        if (_cache.TryGetValue(cacheKey, out var cachedBitmap))
        {
            return cachedBitmap;
        }

        return await _pendingTasks.GetOrAdd(cacheKey, key => Task.Run(() =>
        {
            try
            {
                var bitmap = GenerateThumbnail(imagePath, maxPixelSize);
                if (bitmap != null)
                {
                    bitmap.Freeze(); // UI 스레드에서 안전하게 소유 가능하도록 동결
                    _cache[cacheKey] = bitmap;
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                Log.App.Error($"Failed to generate thumbnail for {imagePath}: {ex.Message}");
                return null;
            }
            finally
            {
                _pendingTasks.TryRemove(cacheKey, out _);
            }
        }));
    }

    public void Prefetch(string imagePath, int maxPixelSize = 360)
    {
        _ = LoadThumbnailAsync(imagePath, maxPixelSize);
    }

    public void ClearCache()
    {
        _cache.Clear();
        _pendingTasks.Clear();
        Log.App.Info("Thumbnail cache cleared");
    }

    public static Image LoadImageUniversal(string path)
    {
        try
        {
            return Image.Load(path);
        }
        catch
        {
            var uri = new Uri(path);
            var decoder = BitmapDecoder.Create(uri, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0) throw;

            var frame = decoder.Frames[0];
            using var ms = new MemoryStream();
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(frame);
            encoder.Save(ms);
            ms.Position = 0;

            return Image.Load(ms);
        }
    }

    private static BitmapSource? GenerateThumbnail(string imagePath, int maxPixelSize)
    {
        try
        {
            using var image = LoadImageUniversal(imagePath);

            int width = image.Width;
            int height = image.Height;

            if (width > maxPixelSize || height > maxPixelSize)
            {
                if (width > height)
                {
                    height = (int)((double)height / width * maxPixelSize);
                    width = maxPixelSize;
                }
                else
                {
                    width = (int)((double)width / height * maxPixelSize);
                    height = maxPixelSize;
                }

                image.Mutate(x => x.Resize(width, height, KnownResamplers.Lanczos3));
            }

            using var memoryStream = new MemoryStream();
            image.SaveAsBmp(memoryStream);
            memoryStream.Position = 0;

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = memoryStream;
            bitmap.EndInit();

            return bitmap;
        }
        catch (Exception ex)
        {
            Log.App.Warn($"ImageSharp error on {imagePath}: {ex.Message}");
            return null;
        }
    }
}
