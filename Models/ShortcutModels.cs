using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShortcutHUD.Models;

public sealed class ShortcutRoot
{
    [JsonPropertyName("categories")]
    public List<ShortcutCategory> Categories { get; set; } = new();
}

public sealed class ShortcutCategory
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<ShortcutItem> Items { get; set; } = new();
}

public sealed class ShortcutItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("keys")]
    public string Keys { get; set; } = string.Empty;

    [JsonPropertyName("note")]
    public string Note { get; set; } = string.Empty;
}

public sealed class ShortcutCategoryView
{
    public string Name { get; set; } = string.Empty;
    public List<ShortcutItem> Items { get; set; } = new();
}

public sealed class AppSettings
{
    public double Opacity { get; set; } = 0.92;
    public bool IsPinned { get; set; } = false;
    public double WindowLeft { get; set; } = 60;
    public double WindowTop { get; set; } = 60;

    public static AppSettings CreateDefault()
    {
        return new AppSettings();
    }
}
