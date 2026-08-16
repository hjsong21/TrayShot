using System;
using System.IO;
using TrayShot.Infrastructure;

namespace TrayShot.Semantic;

public sealed class SemanticEncoder
{
    public bool IsModelLoaded { get; private set; }

    public SemanticEncoder()
    {
        IsModelLoaded = false;
    }

    public float[]? EncodeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;

        // Mock 512-dimension text vector embedding fallback
        var vector = new float[512];
        var tokens = ClipTokenizer.Tokenize(text);
        var rand = new Random(text.GetHashCode());

        for (int i = 0; i < 512; i++)
        {
            vector[i] = (float)(rand.NextDouble() * 2.0 - 1.0);
        }

        return vector;
    }

    public float[]? EncodeImage(string imagePath)
    {
        if (!File.Exists(imagePath)) return null;

        // Mock 512-dimension image vector embedding fallback
        var vector = new float[512];
        var rand = new Random(imagePath.GetHashCode());

        for (int i = 0; i < 512; i++)
        {
            vector[i] = (float)(rand.NextDouble() * 2.0 - 1.0);
        }

        return vector;
    }
}
