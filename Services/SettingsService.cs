using System;
using System.IO;
using System.Text.Json;
using ShortcutHUD.Models;

namespace ShortcutHUD.Services;

public sealed class SettingsService
{
    private const double MinOpacity = 0.2;
    private const double MaxOpacity = 1.0;
    private const double DefaultLeft = 60;
    private const double DefaultTop = 60;
    private const double MinUiFontSize = 10;
    private const double MaxUiFontSize = 20;
    private const double DefaultUiFontSize = 12;
    private const double MinWindowHeight = 44;
    private const double MaxWindowHeight = 180;
    private const double DefaultMinWindowHeight = 52;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string SettingsDirectoryPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ShortcutHUD");

    private static string SettingsFilePath => Path.Combine(SettingsDirectoryPath, "settings.json");

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return AppSettings.CreateDefault();
            }

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            if (settings is null)
            {
                return AppSettings.CreateDefault();
            }

            settings.Opacity = Math.Clamp(settings.Opacity, MinOpacity, MaxOpacity);

            if (!double.IsFinite(settings.WindowLeft))
            {
                settings.WindowLeft = DefaultLeft;
            }

            if (!double.IsFinite(settings.WindowTop))
            {
                settings.WindowTop = DefaultTop;
            }

            if (!double.IsFinite(settings.UiFontSize))
            {
                settings.UiFontSize = DefaultUiFontSize;
            }
            settings.UiFontSize = Math.Clamp(settings.UiFontSize, MinUiFontSize, MaxUiFontSize);

            if (!double.IsFinite(settings.MinWindowHeight))
            {
                settings.MinWindowHeight = DefaultMinWindowHeight;
            }
            settings.MinWindowHeight = Math.Clamp(settings.MinWindowHeight, MinWindowHeight, MaxWindowHeight);

            return settings;
        }
        catch
        {
            return AppSettings.CreateDefault();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(SettingsDirectoryPath);

            var safe = new AppSettings
            {
                Opacity = Math.Clamp(settings.Opacity, MinOpacity, MaxOpacity),
                IsPinned = settings.IsPinned,
                WindowLeft = double.IsFinite(settings.WindowLeft) ? settings.WindowLeft : DefaultLeft,
                WindowTop = double.IsFinite(settings.WindowTop) ? settings.WindowTop : DefaultTop,
                UiFontSize = double.IsFinite(settings.UiFontSize)
                    ? Math.Clamp(settings.UiFontSize, MinUiFontSize, MaxUiFontSize)
                    : DefaultUiFontSize,
                MinWindowHeight = double.IsFinite(settings.MinWindowHeight)
                    ? Math.Clamp(settings.MinWindowHeight, MinWindowHeight, MaxWindowHeight)
                    : DefaultMinWindowHeight
            };

            var json = JsonSerializer.Serialize(safe, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
        }
    }
}
