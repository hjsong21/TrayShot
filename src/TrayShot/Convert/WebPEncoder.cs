using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace TrayShot.Convert;

public static class WebPEncoder
{
    public static void EncodeLossless(string sourcePngPath, string destinationWebpPath)
    {
        using var image = Image.Load(sourcePngPath);

        var encoder = new WebpEncoder
        {
            FileFormat = WebpFileFormatType.Lossless,
            Quality = 100
        };

        using var outputStream = File.Create(destinationWebpPath);
        image.Save(outputStream, encoder);
    }
}
