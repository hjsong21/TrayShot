using System.Threading;
using System.Windows;

namespace TrayShot;

public partial class App : Application
{
    private static Mutex? _mutex;
    private const string MutexName = "Global\\TrayShot_SingleInstance_Mutex";

    protected override void OnStartup(StartupEventArgs e)
    {
        _mutex = new Mutex(true, MutexName, out bool isNewInstance);
        if (!isNewInstance)
        {
            MessageBox.Show("TrayShot가 이미 실행 중입니다.", "TrayShot", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        base.OnStartup(e);
        Infrastructure.LanguageManager.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mutex?.ReleaseMutex();
        _mutex?.Dispose();
        base.OnExit(e);
    }
}

