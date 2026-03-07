using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentTasks.Core.Models;
using FluentTasks.UI.Services;
using Microsoft.UI.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FluentTasks.UI.ViewModels;

/// <summary>
/// ViewModel for the Settings dialog.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject, IDisposable
{
    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private int _selectedThemeIndex;

    [ObservableProperty]
    private LanguageInfo? _selectedLanguage;

    [ObservableProperty]
    private int _selectedDefaultFilterIndex;

    [ObservableProperty]
    private int _selectedDefaultSortIndex;

    [ObservableProperty]
    private ObservableCollection<LanguageInfo> _availableLanguages = [];

    [ObservableProperty]
    private string _copyrightText = string.Empty;

    [ObservableProperty]
    private bool _isRestartDialogOpen;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadLanguages();
        LoadSettings();
        LoadCopyrightText();

        // Subscribe to language changes
        _settingsService.LanguageChanged += OnLanguageChanged;
    }

    private void LoadSettings()
    {
        // Load theme (0=Light, 1=Dark, 2=Default)
        SelectedThemeIndex = _settingsService.AppTheme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            ElementTheme.Default => 2,
            _ => 2
        };

        // Load default filter
        SelectedDefaultFilterIndex = _settingsService.DefaultFilter switch
        {
            FilterOption.All => 0,
            FilterOption.Incomplete => 1,
            FilterOption.Completed => 2,
            FilterOption.Overdue => 3,
            FilterOption.Today => 4,
            FilterOption.ThisWeek => 5,
            _ => 1
        };

        // Load default sort
        SelectedDefaultSortIndex = _settingsService.DefaultSort switch
        {
            SortOption.None => 0,
            SortOption.DueDateAscending => 1,
            SortOption.DueDateDescending => 2,
            SortOption.Alphabetical => 3,
            SortOption.AlphabeticalReverse => 4,
            SortOption.CompletedLast => 5,
            _ => 0
        };

        // Load language
        var language = _settingsService.AppLanguage;
        SelectedLanguage = AvailableLanguages.FirstOrDefault(l => l.Code == language)
            ?? AvailableLanguages.FirstOrDefault(l => l.Code == "auto");
    }

    private void LoadLanguages()
    {
        AvailableLanguages = new ObservableCollection<LanguageInfo>(LanguageService.GetSupportedLanguages());
    }

    private void LoadCopyrightText()
    {
        var currentYear = DateTime.Now.Year;
        CopyrightText = string.Format("© {0} Javier Ferrer. All rights reserved.", currentYear);
    }

    [RelayCommand]
    private void SetTheme(int selectedIndex)
    {
        var theme = selectedIndex switch
        {
            0 => ElementTheme.Light,
            1 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        _settingsService.AppTheme = theme;
    }

    [RelayCommand]
    private void SetDefaultFilter(int selectedIndex)
    {
        var filter = selectedIndex switch
        {
            0 => FilterOption.All,
            1 => FilterOption.Incomplete,
            2 => FilterOption.Completed,
            3 => FilterOption.Overdue,
            4 => FilterOption.Today,
            5 => FilterOption.ThisWeek,
            _ => FilterOption.Incomplete
        };

        _settingsService.DefaultFilter = filter;
    }

    [RelayCommand]
    private void SetDefaultSort(int selectedIndex)
    {
        var sort = selectedIndex switch
        {
            0 => SortOption.None,
            1 => SortOption.DueDateAscending,
            2 => SortOption.DueDateDescending,
            3 => SortOption.Alphabetical,
            4 => SortOption.AlphabeticalReverse,
            5 => SortOption.CompletedLast,
            _ => SortOption.None
        };

        _settingsService.DefaultSort = sort;
    }

    partial void OnSelectedLanguageChanged(LanguageInfo? value)
    {
        if (value is null || IsRestartDialogOpen)
            return;

        // Ignore no-op selections
        if (string.Equals(_settingsService.AppLanguage, value.Code, StringComparison.OrdinalIgnoreCase))
            return;

        _settingsService.AppLanguage = value.Code;
    }

    private async void OnLanguageChanged(object? sender, string e)
    {
        await ShowRestartNotificationAsync();
    }

    private async Task ShowRestartNotificationAsync()
    {
        // Event handler for language change - view will show dialog
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Event raised when language changes and restart is needed.
    /// </summary>
    public event EventHandler? LanguageChanged;

    public async Task RestartWithNewLanguageAsync()
    {
        var effectiveLanguage = LanguageService.GetEffectiveLanguage(_settingsService.AppLanguage);
        var restartArgs = $"--lang={effectiveLanguage}";
        Microsoft.Windows.AppLifecycle.AppInstance.Restart(restartArgs);
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        await _settingsService.ClearCacheAsync();
    }

    [RelayCommand]
    private async Task LogOutAsync()
    {
        await _settingsService.LogOutAsync();
    }

    public void Dispose()
    {
        _settingsService.LanguageChanged -= OnLanguageChanged;
    }
}
