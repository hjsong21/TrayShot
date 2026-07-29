using System;
using System.IO;
using System.Threading.Tasks;
using Sukurini.Search;
using Xunit;

namespace Sukurini.Tests.Search;

public class SearchIndexTests : IDisposable
{
    private readonly string _tempDbPath;

    public SearchIndexTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"sukurini_search_{Guid.NewGuid():N}.sqlite");
    }

    [Fact]
    public async Task SearchIndex_IndexesTextAndSearchesWithFts5AndLike()
    {
        using var index = new SearchIndex(_tempDbPath);

        string sampleFile = Path.Combine(Path.GetTempPath(), "sample_ocr_shot.png");
        File.WriteAllText(sampleFile, "dummy img content");

        try
        {
            await index.IndexOcrTextAsync(sampleFile, "Sukurini 스크린샷 텍스트 검색 엔진 테스트");

            // FTS5 또는 LIKE 검색 검증
            var results = await index.SearchAsync("스크린샷");

            Assert.Single(results);
            Assert.Equal(sampleFile, results[0]);
        }
        finally
        {
            if (File.Exists(sampleFile)) File.Delete(sampleFile);
        }
    }

    public void Dispose()
    {
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
    }
}
