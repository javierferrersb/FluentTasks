using System;
using System.Threading.Tasks;
using FluentTasks.Core.Models;
using FluentTasks.Infrastructure.Google;
using Microsoft.UI.Xaml;
using Windows.Storage;

namespace FluentTasks.UI.Services;

/// <summary>
/// Service for managing application settings and preferences.
/// Uses ApplicationData.LocalSettings for persistence.
/// </summary>
public sealed class SettingsService
{
    private readonly ApplicationDataContainer _localSettings;
    private readonly IGoogleAuthService _authService;
    private Window? _mainWindow;

    /// <summary>
    /// Raised when the application theme changes.
    /// </summary>
    public event EventHandler<ElementTheme>? ThemeChanged;

    /// <summary>
    /// Raised when the user logs out and the app should restart.
    /// </summary>
    public event EventHandler? LoggedOut;

    // Settings keys
    private const string ThemeKey = "AppTheme";
    private const string DefaultFilterKey = "DefaultFilter";
    private const string DefaultSortKey = "DefaultSort";

    public SettingsService(IGoogleAuthService authService)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _localSettings = ApplicationData.Current.LocalSettings;
    }

    /// <summary>
    /// Sets the main window reference for theme changes.
    /// </summary>
    public void SetMainWindow(Window window)
    {
        _mainWindow = window;
    }

    /// <summary>
    /// Gets or sets the application theme.
    /// </summary>
    public ElementTheme AppTheme
    {
        get
        {
            if (_localSettings.Values[ThemeKey] is int themeInt)
            {
                return (ElementTheme)themeInt;
            }
            return ElementTheme.Default;
        }
        set
        {
            _localSettings.Values[ThemeKey] = (int)value;
            ApplyTheme(value);
        }
    }

    /// <summary>
    /// Gets or sets the default filter option.
    /// </summary>
    public FilterOption DefaultFilter
    {
        get
        {
            if (_localSettings.Values[DefaultFilterKey] is int filterInt)
            {
                return (FilterOption)filterInt;
            }
            return FilterOption.Incomplete;
        }
        set => _localSettings.Values[DefaultFilterKey] = (int)value;
    }

    /// <summary>
    /// Gets or sets the default sort option.
    /// </summary>
    public SortOption DefaultSort
    {
        get
        {
            if (_localSettings.Values[DefaultSortKey] is int sortInt)
            {
                return (SortOption)sortInt;
            }
            return SortOption.None;
        }
        set => _localSettings.Values[DefaultSortKey] = (int)value;
    }

    /// <summary>
    /// Applies the saved theme to the application.
    /// </summary>
    public void InitializeTheme()
    {
        ApplyTheme(AppTheme);
    }

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    public async Task ClearCacheAsync()
    {
        try
        {
            // Clear local folder cache
            var localFolder = ApplicationData.Current.LocalFolder;
            var files = await localFolder.GetFilesAsync();
            foreach (var file in files)
            {
                try
                {
                    await file.DeleteAsync();
                }
                catch
                {
                    // Continue on error
                }
            }
        }
        catch
        {
            // Silently fail if cache clearing fails
        }
    }

    /// <summary>
    /// Logs out the current user by clearing auth tokens and app data.
    /// </summary>
    public async Task LogOutAsync()
    {
        // Clear all settings except theme preference
        var savedTheme = AppTheme;

        _localSettings.Values.Clear();

        // Restore theme preference
        AppTheme = savedTheme;

        // Clear cache
        await ClearCacheAsync();

        // Log out from Google Auth service
        await _authService.LogOutAsync();

        // Notify listeners that user has logged out
        LoggedOut?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyTheme(ElementTheme theme)
    {
        if (_mainWindow?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = theme;
        }

        // Notify listeners that theme has changed
        ThemeChanged?.Invoke(this, theme);
    }
}
