using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FluentTasks.UI.Services;

/// <summary>
/// Stores custom icons for task lists locally
/// </summary>
public class IconStorageService
{
    private readonly string _iconFilePath;
    private Dictionary<string, string> _icons = new();

    public IconStorageService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appFolder = Path.Combine(appDataPath, "FluentTasks");
        Directory.CreateDirectory(appFolder);
        _iconFilePath = Path.Combine(appFolder, "list_icons.json");

        LoadIcons();
    }

    public string GetIcon(string listId)
    {
        return _icons.TryGetValue(listId, out var icon) ? icon : "\uE8F4"; // Default list icon
    }

    public void SetIcon(string listId, string icon)
    {
        _icons[listId] = icon;
        SaveIcons();
    }

    private void LoadIcons()
    {
        try
        {
            if (File.Exists(_iconFilePath))
            {
                var json = File.ReadAllText(_iconFilePath);
                _icons = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch
        {
            _icons = new();
        }
    }

    private void SaveIcons()
    {
        try
        {
            var json = JsonSerializer.Serialize(_icons, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_iconFilePath, json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}