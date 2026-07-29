using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Sukurini.Infrastructure;

namespace Sukurini.Semantic;

public record VectorSearchResult(string Path, float Similarity);

public sealed class VectorStore
{
    private readonly ConcurrentDictionary<string, float[]> _vectors = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _vectors.Count;

    public void AddOrUpdate(string path, float[] embedding)
    {
        if (embedding == null || embedding.Length != 512)
        {
            Log.Semantic.Warn($"Invalid embedding dimension for {path}");
            return;
        }

        _vectors[path] = Normalize(embedding);
    }

    public void Remove(string path)
    {
        _vectors.TryRemove(path, out _);
    }

    public List<VectorSearchResult> Search(float[] queryEmbedding, int topK = 120, float minSimilarity = 0.15f)
    {
        if (queryEmbedding == null || queryEmbedding.Length != 512 || _vectors.IsEmpty)
            return new List<VectorSearchResult>();

        var normalizedQuery = Normalize(queryEmbedding);
        var results = new List<VectorSearchResult>();

        foreach (var kvp in _vectors)
        {
            float sim = CosineSimilarity(normalizedQuery, kvp.Value);
            if (sim >= minSimilarity)
            {
                results.Add(new VectorSearchResult(kvp.Key, sim));
            }
        }

        return results.OrderByDescending(r => r.Similarity).Take(topK).ToList();
    }

    private static float[] Normalize(float[] v)
    {
        double sumSq = 0;
        for (int i = 0; i < v.Length; i++) sumSq += v[i] * v[i];
        float norm = (float)Math.Sqrt(sumSq);

        if (norm < 1e-6f) return v;

        var result = new float[v.Length];
        for (int i = 0; i < v.Length; i++) result[i] = v[i] / norm;
        return result;
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
        }
        return dot;
    }
}
