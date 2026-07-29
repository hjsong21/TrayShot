using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sukurini.Search;

namespace Sukurini.Semantic;

public sealed class HybridSearchProvider
{
    private readonly SearchIndex _searchIndex;
    private readonly VectorStore _vectorStore;
    private readonly SemanticEncoder _semanticEncoder;

    public HybridSearchProvider(SearchIndex searchIndex, VectorStore vectorStore, SemanticEncoder semanticEncoder)
    {
        _searchIndex = searchIndex;
        _vectorStore = vectorStore;
        _semanticEncoder = semanticEncoder;
    }

    public async Task<List<string>> SearchAsync(string query)
    {
        var ocrResults = await _searchIndex.SearchAsync(query);
        var queryEmbedding = _semanticEncoder.EncodeText(query);

        if (queryEmbedding == null) return ocrResults;

        var semanticResults = _vectorStore.Search(queryEmbedding, topK: 120);

        var combined = new HashSet<string>(ocrResults, StringComparer.OrdinalIgnoreCase);
        foreach (var sr in semanticResults)
        {
            combined.Add(sr.Path);
        }

        return combined.ToList();
    }
}
