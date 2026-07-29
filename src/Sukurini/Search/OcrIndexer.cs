using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Sukurini.Infrastructure;
using Windows.Storage;

namespace Sukurini.Search;

public sealed class OcrIndexer
{
    private readonly SearchIndex _searchIndex;
    private OcrEngine? _ocrEngine;

    public OcrIndexer(SearchIndex searchIndex)
    {
        _searchIndex = searchIndex;
        InitializeOcrEngine();
    }

    private void InitializeOcrEngine()
    {
        try
        {
            // 1차: 한국어 지원 여부 확인
            var koLang = new Language("ko-KR");
            if (OcrEngine.IsLanguageSupported(koLang))
            {
                _ocrEngine = OcrEngine.TryCreateFromLanguage(koLang);
            }
            else
            {
                var available = OcrEngine.AvailableRecognizerLanguages;
                if (available.Count > 0)
                {
                    _ocrEngine = OcrEngine.TryCreateFromLanguage(available[0]);
                }
            }

            Log.Ocr.Info($"WinRT OcrEngine initialized language={_ocrEngine?.RecognizerLanguage?.LanguageTag ?? "none"}");
        }
        catch (Exception ex)
        {
            Log.Ocr.Error($"Failed to initialize WinRT OcrEngine: {ex.Message}");
        }
    }

    public async Task<string?> ProcessFileAsync(string filePath)
    {
        if (_ocrEngine == null || !File.Exists(filePath))
            return null;

        try
        {
            var storageFile = await StorageFile.GetFileFromPathAsync(filePath);
            using var stream = await storageFile.OpenAsync(FileAccessMode.Read);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            var ocrResult = await _ocrEngine.RecognizeAsync(softwareBitmap);
            string text = ocrResult.Text ?? string.Empty;

            await _searchIndex.IndexOcrTextAsync(filePath, text);
            return text;
        }
        catch (Exception ex)
        {
            Log.Ocr.Warn($"OCR extraction failed for {filePath}: {ex.Message}");
            return null;
        }
    }
}
