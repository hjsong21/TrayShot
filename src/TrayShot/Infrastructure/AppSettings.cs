using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrayShot.Infrastructure;

public enum WebPDisposal
{
    Trash,
    Delete,
    Keep
}

public enum GallerySortOrder
{
    Relevance,
    Newest,
    Oldest
}

public enum AppTheme
{
    Dark,
    Light,
    System
}

public record HotKeyBinding(uint KeyCode, uint Modifiers);

public sealed class AppSettings : INotifyPropertyChanged
{
    private static readonly Lazy<AppSettings> _instance = new(() => new AppSettings());
    public static AppSettings Shared => _instance.Value;

    public event PropertyChangedEventHandler? PropertyChanged;

    public event Action? FoldersChanged;
    public event Action? ActiveFolderChanged;
    public event Action? OcrEnabledChanged;
    public event Action? OcrPowerPolicyChanged;
    public event Action? WebpConversionChanged;
    public event Action? OrganizeChanged;
    public event Action? ThemeChanged;
    public event Action? LanguageChanged;
    public event Action? IncludeSubfoldersChanged;
    public event Action? SemanticEnabledChanged;
    public event Action? SemanticModelChanged;
    public event Action? AnalyticsEnabledChanged;
    public event Action? UpdateChannelChanged;
    public event Action? HotKeyChanged;

    private readonly string _settingsFilePath;
    private SettingsData _data;

    public bool IsFirstLaunch { get; private set; }

    public AppSettings(string? customPath = null)
    {
        _settingsFilePath = customPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TrayShot",
            "settings.json");

        _data = LoadSettings();
        RecordLaunch();
        SeedIfNeeded();
    }

    private class SettingsData
    {
        public List<string> Folders { get; set; } = new();
        public string? ActiveFolder { get; set; }
        public bool OcrEnabled { get; set; } = true;
        public bool LazyIndexOnBattery { get; set; } = true;
        public bool PauseIndexingOnLowPower { get; set; } = true;
        public bool WebpConversionEnabled { get; set; } = false;
        public DateTime? WebpConversionEnabledAt { get; set; }
        public WebPDisposal WebpDisposal { get; set; } = WebPDisposal.Trash;
        public bool CopyAsPngEnabled { get; set; } = false;
        public bool OrganizeEnabled { get; set; } = false;
        public string OrganizeFormat { get; set; } = "yyyy/MM";
        public bool IncludeSubfolders { get; set; } = false;
        public bool SemanticSearchEnabled { get; set; } = false;
        public string SemanticModelIdentifier { get; set; } = "mobileclip-s2";
        public bool AnalyticsEnabled { get; set; } = false;
        public string Language { get; set; } = "system";
        public string UpdateChannel { get; set; } = "stable";
        public GallerySortOrder GallerySortOrder { get; set; } = GallerySortOrder.Relevance;
        public uint? HotKeyCode { get; set; }
        public uint? HotKeyModifiers { get; set; }
        public DateTime? FirstLaunchedAt { get; set; }
        public int LaunchCount { get; set; } = 0;
        public DateTime? OnboardingCompletedAt { get; set; }
        public double GalleryWidth { get; set; } = 664.0;
        public double GalleryHeight { get; set; } = 480.0;
        public AppTheme Theme { get; set; } = AppTheme.Dark;
    }

    private SettingsData LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                string json = File.ReadAllText(_settingsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                return JsonSerializer.Deserialize<SettingsData>(json, options) ?? new SettingsData();
            }
        }
        catch (Exception ex)
        {
            Log.Settings.Error($"Failed to load settings: {ex.Message}");
        }
        return new SettingsData();
    }

    private void SaveSettings()
    {
        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            string json = JsonSerializer.Serialize(_data, options);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch (Exception ex)
        {
            Log.Settings.Error($"Failed to save settings: {ex.Message}");
        }
    }

    public IReadOnlyList<string> Folders
    {
        get => _data.Folders;
        set
        {
            var unique = value.Distinct().ToList();
            if (!_data.Folders.SequenceEqual(unique))
            {
                _data.Folders = unique;
                SaveSettings();
                OnPropertyChanged();
                FoldersChanged?.Invoke();
            }
        }
    }

    public string? ActiveFolder
    {
        get => _data.ActiveFolder;
        set
        {
            if (_data.ActiveFolder != value)
            {
                _data.ActiveFolder = value;
                SaveSettings();
                OnPropertyChanged();
                ActiveFolderChanged?.Invoke();
            }
        }
    }

    public bool OcrEnabled
    {
        get => _data.OcrEnabled;
        set
        {
            if (_data.OcrEnabled != value)
            {
                _data.OcrEnabled = value;
                SaveSettings();
                OnPropertyChanged();
                OcrEnabledChanged?.Invoke();
            }
        }
    }

    public bool LazyIndexOnBattery
    {
        get => _data.LazyIndexOnBattery;
        set
        {
            if (_data.LazyIndexOnBattery != value)
            {
                _data.LazyIndexOnBattery = value;
                SaveSettings();
                OnPropertyChanged();
                OcrPowerPolicyChanged?.Invoke();
            }
        }
    }

    public bool PauseIndexingOnLowPower
    {
        get => _data.PauseIndexingOnLowPower;
        set
        {
            if (_data.PauseIndexingOnLowPower != value)
            {
                _data.PauseIndexingOnLowPower = value;
                SaveSettings();
                OnPropertyChanged();
                OcrPowerPolicyChanged?.Invoke();
            }
        }
    }

    public bool WebpConversionEnabled
    {
        get => _data.WebpConversionEnabled;
        set
        {
            if (_data.WebpConversionEnabled != value)
            {
                _data.WebpConversionEnabled = value;
                if (value)
                {
                    _data.WebpConversionEnabledAt ??= DateTime.UtcNow;
                }
                else
                {
                    _data.WebpConversionEnabledAt = null;
                }
                SaveSettings();
                OnPropertyChanged();
                WebpConversionChanged?.Invoke();
            }
        }
    }

    public DateTime? WebpConversionEnabledAt => _data.WebpConversionEnabledAt;

    public WebPDisposal WebpDisposal
    {
        get => _data.WebpDisposal;
        set
        {
            if (_data.WebpDisposal != value)
            {
                _data.WebpDisposal = value;
                SaveSettings();
                OnPropertyChanged();
                WebpConversionChanged?.Invoke();
            }
        }
    }

    public bool CopyAsPngEnabled
    {
        get => _data.CopyAsPngEnabled;
        set
        {
            if (_data.CopyAsPngEnabled != value)
            {
                _data.CopyAsPngEnabled = value;
                SaveSettings();
                OnPropertyChanged();
                WebpConversionChanged?.Invoke();
            }
        }
    }

    public bool OrganizeEnabled
    {
        get => _data.OrganizeEnabled;
        set
        {
            if (_data.OrganizeEnabled != value)
            {
                _data.OrganizeEnabled = value;
                SaveSettings();
                OnPropertyChanged();
                OrganizeChanged?.Invoke();
            }
        }
    }

    public string OrganizeFormat
    {
        get => _data.OrganizeFormat;
        set
        {
            if (_data.OrganizeFormat != value)
            {
                _data.OrganizeFormat = value;
                SaveSettings();
                OnPropertyChanged();
                OrganizeChanged?.Invoke();
            }
        }
    }

    public bool IncludeSubfolders
    {
        get => _data.IncludeSubfolders;
        set
        {
            if (_data.IncludeSubfolders != value)
            {
                _data.IncludeSubfolders = value;
                SaveSettings();
                OnPropertyChanged();
                IncludeSubfoldersChanged?.Invoke();
            }
        }
    }

    public bool SemanticSearchEnabled
    {
        get => _data.SemanticSearchEnabled;
        set
        {
            if (_data.SemanticSearchEnabled != value)
            {
                _data.SemanticSearchEnabled = value;
                SaveSettings();
                OnPropertyChanged();
                SemanticEnabledChanged?.Invoke();
            }
        }
    }

    public string SemanticModelIdentifier
    {
        get => _data.SemanticModelIdentifier;
        set
        {
            if (_data.SemanticModelIdentifier != value)
            {
                _data.SemanticModelIdentifier = value;
                SaveSettings();
                OnPropertyChanged();
                SemanticModelChanged?.Invoke();
            }
        }
    }

    public bool AnalyticsEnabled
    {
        get => _data.AnalyticsEnabled;
        set
        {
            if (_data.AnalyticsEnabled != value)
            {
                _data.AnalyticsEnabled = value;
                SaveSettings();
                OnPropertyChanged();
                AnalyticsEnabledChanged?.Invoke();
            }
        }
    }

    public string Language
    {
        get => _data.Language;
        set
        {
            if (_data.Language != value)
            {
                _data.Language = value;
                SaveSettings();
                OnPropertyChanged();
                LanguageManager.ApplyLanguage(value);
                LanguageChanged?.Invoke();
            }
        }
    }

    public string UpdateChannel
    {
        get => _data.UpdateChannel;
        set
        {
            if (_data.UpdateChannel != value)
            {
                _data.UpdateChannel = value;
                SaveSettings();
                OnPropertyChanged();
                UpdateChannelChanged?.Invoke();
            }
        }
    }

    public GallerySortOrder GallerySortOrder
    {
        get => _data.GallerySortOrder;
        set
        {
            if (_data.GallerySortOrder != value)
            {
                _data.GallerySortOrder = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public HotKeyBinding GalleryHotKey
    {
        get
        {
            uint key = _data.HotKeyCode ?? 0x53; // 'S'
            uint mod = _data.HotKeyModifiers ?? 0x0003; // Ctrl (2) + Alt (1) = 3
            return new HotKeyBinding(key, mod);
        }
        set
        {
            _data.HotKeyCode = value.KeyCode;
            _data.HotKeyModifiers = value.Modifiers;
            SaveSettings();
            OnPropertyChanged();
            HotKeyChanged?.Invoke();
        }
    }

    public void AddFolder(string folderPath)
    {
        var current = Folders.ToList();
        if (!current.Contains(folderPath, StringComparer.OrdinalIgnoreCase))
        {
            current.Add(folderPath);
            Folders = current;
            ActiveFolder = folderPath;
        }
    }

    public void RemoveFolder(string folderPath)
    {
        var current = Folders.Where(f => !f.Equals(folderPath, StringComparison.OrdinalIgnoreCase)).ToList();
        Folders = current;
        if (ActiveFolder?.Equals(folderPath, StringComparison.OrdinalIgnoreCase) == true)
        {
            ActiveFolder = current.FirstOrDefault();
        }
    }

    private void RecordLaunch()
    {
        _data.LaunchCount++;
        if (!_data.FirstLaunchedAt.HasValue)
        {
            _data.FirstLaunchedAt = DateTime.UtcNow;
            IsFirstLaunch = true;
        }
        SaveSettings();
    }

    private void SeedIfNeeded()
    {
        if (_data.Folders.Count == 0)
        {
            var defaultPictures = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                "Screenshots");
            if (!Directory.Exists(defaultPictures))
            {
                defaultPictures = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            }

            _data.Folders.Add(defaultPictures);
            _data.ActiveFolder = defaultPictures;
            SaveSettings();
        }
    }

    public double GalleryWidth
    {
        get => _data.GalleryWidth;
        set
        {
            if (Math.Abs(_data.GalleryWidth - value) > 1.0)
            {
                _data.GalleryWidth = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public double GalleryHeight
    {
        get => _data.GalleryHeight;
        set
        {
            if (Math.Abs(_data.GalleryHeight - value) > 1.0)
            {
                _data.GalleryHeight = value;
                SaveSettings();
                OnPropertyChanged();
            }
        }
    }

    public AppTheme Theme
    {
        get => _data.Theme;
        set
        {
            if (_data.Theme != value)
            {
                _data.Theme = value;
                SaveSettings();
                OnPropertyChanged();
                ThemeChanged?.Invoke();
            }
        }
    }

    public void ResetGallerySizeToDefault()
    {
        GalleryWidth = 664.0;
        GalleryHeight = 480.0;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
