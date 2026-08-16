using TrayShot.Semantic;
using Xunit;

namespace TrayShot.Tests.Semantic;

public class VectorStoreTests
{
    [Fact]
    public void VectorStore_AddsAndSearchesVectorsCorrectly()
    {
        var store = new VectorStore();
        var vecA = new float[512];
        var vecB = new float[512];

        vecA[0] = 1.0f;
        vecB[0] = 0.9f;
        vecB[1] = 0.1f;

        store.AddOrUpdate(@"C:\PathA.png", vecA);

        var results = store.Search(vecB, topK: 10, minSimilarity: 0.5f);

        Assert.Single(results);
        Assert.Equal(@"C:\PathA.png", results[0].Path);
        Assert.True(results[0].Similarity > 0.8f);
    }
}
