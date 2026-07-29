using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sukurini.Core;
using Sukurini.Infrastructure;
using Sukurini.Models;

namespace Sukurini.Convert;

public record ConversionJob(string SourcePngPath, ConversionOrigin Origin);

public sealed class ConversionCoordinator
{
    private static readonly Lazy<ConversionCoordinator> _instance = new(() => new ConversionCoordinator());
    public static ConversionCoordinator Shared => _instance.Value;

    private readonly ScreenshotConverter _converter = new();
    private readonly Channel<ConversionJob> _jobChannel = Channel.CreateUnbounded<ConversionJob>();
    private readonly ConcurrentDictionary<string, byte> _enqueuedPaths = new(StringComparer.OrdinalIgnoreCase);

    public ConversionCoordinator()
    {
        Task.Run(ProcessQueueAsync);
    }

    public void Initialize()
    {
        ScreenshotStore.Shared.Changed += OnStoreChanged;
        AppSettings.Shared.WebpConversionChanged += OnSettingsChanged;

        // 초기 시작 시 설정이 켜져있다면 현재 스토어의 모든 PNG에 대해 백필 변환 시도
        CheckAndEnqueueExistingPngs();

        Log.Convert.Info("ConversionCoordinator initialized and observing events");
    }

    public void Enqueue(string sourcePngPath, ConversionOrigin origin)
    {
        if (!AppSettings.Shared.WebpConversionEnabled)
            return;

        if (ScreenshotFile.IsConvertible(sourcePngPath))
        {
            if (_enqueuedPaths.TryAdd(sourcePngPath, 0))
            {
                _jobChannel.Writer.TryWrite(new ConversionJob(sourcePngPath, origin));
                Log.Convert.Debug($"Enqueued PNG for WebP conversion: {sourcePngPath} origin={origin}");
            }
        }
    }

    private void OnStoreChanged(StoreChange change)
    {
        if (!AppSettings.Shared.WebpConversionEnabled)
            return;

        // 1. 새 스크린샷 추가 시 자동 변환 (Live)
        foreach (var item in change.Inserted)
        {
            Enqueue(item.Path, ConversionOrigin.Live);
        }

        // 2. 전체 스캔/기존 PNG 스크린샷 검사 (Backfill)
        CheckAndEnqueueExistingPngs();
    }

    private void OnSettingsChanged()
    {
        if (AppSettings.Shared.WebpConversionEnabled)
        {
            CheckAndEnqueueExistingPngs();
        }
    }

    private void CheckAndEnqueueExistingPngs()
    {
        if (!AppSettings.Shared.WebpConversionEnabled)
            return;

        var items = ScreenshotStore.Shared.Items;
        foreach (var item in items)
        {
            if (ScreenshotFile.IsConvertible(item.Path))
            {
                Enqueue(item.Path, ConversionOrigin.Backfill);
            }
        }
    }

    private async Task ProcessQueueAsync()
    {
        while (await _jobChannel.Reader.WaitToReadAsync())
        {
            while (_jobChannel.Reader.TryRead(out var job))
            {
                var holdToken = ScreenshotStore.Shared.AddConversionHold(
                    job.SourcePngPath,
                    ScreenshotFile.ConvertedPath(job.SourcePngPath),
                    job.Origin);

                try
                {
                    bool success = _converter.ConvertAndVerify(job.SourcePngPath, out string webpPath);
                    if (success)
                    {
                        ScreenshotStore.Shared.TriggerScan();
                    }
                }
                catch (Exception ex)
                {
                    Log.Convert.Error($"Conversion process error: {ex.Message}");
                }
                finally
                {
                    ScreenshotStore.Shared.RemoveConversionHold(holdToken);
                    _enqueuedPaths.TryRemove(job.SourcePngPath, out _);
                }
            }
        }
    }
}
