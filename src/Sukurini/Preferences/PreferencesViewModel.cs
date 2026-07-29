using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Sukurini.Core;
using Sukurini.Infrastructure;

namespace Sukurini.Preferences;

public partial class PreferencesViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _launchAtStartup;

    [ObservableProperty]
    private bool _ocrEnabled;

    [ObservableProperty]
    private bool _webpConversionEnabled;

    [ObservableProperty]
    private bool _semanticSearchEnabled;

    public ObservableCollection<string> MonitoredFolders { get; } = new();

    public PreferencesViewModel()
    {
        _launchAtStartup = StartupManager.IsStartupEnabled();
        _ocrEnabled = AppSettings.Shared.OcrEnabled;
        _webpConversionEnabled = AppSettings.Shared.WebpConversionEnabled;
        _semanticSearchEnabled = AppSettings.Shared.SemanticSearchEnabled;

        foreach (var folder in AppSettings.Shared.Folders)
        {
            MonitoredFolders.Add(folder);
        }
    }

    partial void OnLaunchAtStartupChanged(bool value)
    {
        StartupManager.SetStartupEnabled(value);
    }

    partial void OnOcrEnabledChanged(bool value)
    {
        AppSettings.Shared.OcrEnabled = value;
    }

    partial void OnWebpConversionEnabledChanged(bool value)
    {
        AppSettings.Shared.WebpConversionEnabled = value;
    }

    partial void OnSemanticSearchEnabledChanged(bool value)
    {
        AppSettings.Shared.SemanticSearchEnabled = value;
    }
}
