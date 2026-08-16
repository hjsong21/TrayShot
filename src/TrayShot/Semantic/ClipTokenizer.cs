using System;
using System.Collections.Generic;

namespace TrayShot.Semantic;

public sealed class ClipTokenizer
{
    public const int ContextLength = 77;
    public const long StartOfText = 49406;
    public const long EndOfText = 49407;

    public static long[] Tokenize(string text)
    {
        var tokens = new List<long> { StartOfText };

        if (!string.IsNullOrWhiteSpace(text))
        {
            // Simple word token hashing simulation for 512d CLIP compatibility fallback
            foreach (var ch in text)
            {
                long val = (long)ch % 40000 + 100;
                tokens.Add(val);
                if (tokens.Count >= ContextLength - 1) break;
            }
        }

        tokens.Add(EndOfText);

        var result = new long[ContextLength];
        for (int i = 0; i < ContextLength; i++)
        {
            result[i] = i < tokens.Count ? tokens[i] : 0;
        }

        return result;
    }
}
