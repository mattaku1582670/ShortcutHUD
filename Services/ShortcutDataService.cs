using System;
using System.IO;
using System.Text.Json;
using ShortcutHUD.Models;

namespace ShortcutHUD.Services;

public sealed class ShortcutDataService
{
    private const string FileName = "shortcuts.json";

    public ShortcutDataLoadResult LoadFromExecutableFolder()
    {
        var path = Path.Combine(AppContext.BaseDirectory, FileName);

        if (!File.Exists(path))
        {
            return new ShortcutDataLoadResult
            {
                Data = new ShortcutRoot(),
                ErrorMessage = "shortcuts.json が見つかりません。実行ファイルと同じフォルダに配置してください。"
            };
        }

        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<ShortcutRoot>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (data is null)
            {
                return new ShortcutDataLoadResult
                {
                    Data = new ShortcutRoot(),
                    ErrorMessage = "shortcuts.json を読み込めませんでした。"
                };
            }

            data.Categories ??= new();

            foreach (var category in data.Categories)
            {
                category.Name ??= string.Empty;
                category.Items ??= new();

                foreach (var item in category.Items)
                {
                    item.Name ??= string.Empty;
                    item.Keys ??= string.Empty;
                    item.Note ??= string.Empty;
                }
            }

            return new ShortcutDataLoadResult
            {
                Data = data
            };
        }
        catch (Exception)
        {
            return new ShortcutDataLoadResult
            {
                Data = new ShortcutRoot(),
                ErrorMessage = "shortcuts.json の形式が不正です。JSONを確認してください。"
            };
        }
    }
}

public sealed class ShortcutDataLoadResult
{
    public ShortcutRoot Data { get; set; } = new();
    public string? ErrorMessage { get; set; }
}
