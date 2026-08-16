using System.Windows.Forms;

namespace TrayShot.Search;

public sealed class OcrPowerPolicy
{
    public int Concurrency { get; set; } = 2;
    public bool IsPaused { get; set; } = false;

    public static OcrPowerPolicy Current()
    {
        var status = SystemInformation.PowerStatus;
        bool isBattery = status.PowerLineStatus == PowerLineStatus.Offline;

        return new OcrPowerPolicy
        {
            Concurrency = isBattery ? 1 : 2,
            IsPaused = false
        };
    }
}
