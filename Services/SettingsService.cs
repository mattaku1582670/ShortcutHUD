using System;
using System.IO;
using System.Text.Json;
using ShortcutHUD.Models;

namespace ShortcutHUD.Services;

public sealed class SettingsService
{
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

            settings.Opacity = Math.Clamp(settings.Opacity, 0.2, 1.0);

            if (!double.IsFinite(settings.WindowLeft))
            {
                settings.WindowLeft = 60;
            }

            if (!double.IsFinite(settings.WindowTop))
            {
                settings.WindowTop = 60;
            }

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
                Opacity = Math.Clamp(settings.Opacity, 0.2, 1.0),
                IsPinned = settings.IsPinned,
                WindowLeft = double.IsFinite(settings.WindowLeft) ? settings.WindowLeft : 60,
                WindowTop = double.IsFinite(settings.WindowTop) ? settings.WindowTop : 60
            };

            var json = JsonSerializer.Serialize(safe, JsonOptions);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch
        {
        }
    }
}
