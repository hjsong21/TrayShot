using System;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Threading.Tasks;
using Sukurini.Core;
using Sukurini.Infrastructure;
using Sukurini.Models;

namespace Sukurini.Convert;

public record ConversionJob(string SourcePngPath, ConversionOrigin Origin);

public sealed class ConversionCoordinator
{
    private readonly ScreenshotConverter _converter = new();
    private readonly Channel<ConversionJob> _jobChannel = Channel.CreateUnbounded<ConversionJob>();

    public ConversionCoordinator()
    {
        Task.Run(ProcessQueueAsync);
    }

    public void Enqueue(string sourcePngPath, ConversionOrigin origin)
    {
        if (ScreenshotFile.IsConvertible(sourcePngPath))
        {
            _jobChannel.Writer.TryWrite(new ConversionJob(sourcePngPath, origin));
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
                    bool success = _converter.ConvertAndVerify(job.SourcePngPath, out _);
                    if (success)
                    {
                        ScreenshotStore.Shared.TriggerScan();
                    }
                }
                finally
                {
                    ScreenshotStore.Shared.RemoveConversionHold(holdToken);
                }
            }
        }
    }
}
