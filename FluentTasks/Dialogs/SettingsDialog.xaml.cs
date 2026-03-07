using FluentTasks.UI.Services;
using FluentTasks.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentTasks.UI.Dialogs;

/// <summary>
/// Unified settings control with all settings on a single page.
/// </summary>
public sealed partial class SettingsDialog : UserControl, IDisposable
{
    private readonly SettingsViewModel _viewModel;
    private readonly ResourceLoader _resourceLoader;
    private bool _isLoading;
    private bool _isShowingRestartDialog;

    /// <summary>
    /// Raised when the hamburger menu button is clicked.
    /// </summary>
    public event EventHandler? HamburgerButtonClicked;

    /// <summary>
    /// Raised when the user requests to see keyboard shortcuts.
    /// </summary>
    public event EventHandler? ShowShortcutsRequested;

    /// <summary>
    /// Whether to show the hamburger menu button in the header.
    /// </summary>
    public bool ShowHamburgerButton
    {
        get => HamburgerButton.Visibility == Visibility.Visible;
        set => HamburgerButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    public SettingsDialog(SettingsViewModel viewModel)
    {
        _viewModel = viewModel;
        _resourceLoader = new ResourceLoader();
        InitializeComponent();

        _isLoading = true;
        LoadLanguages();
        LoadSettings();
        _isLoading = false;

        LoadVersionInfo();

        // Wire up ViewModel events
        _viewModel.LanguageChanged += ViewModel_LanguageChanged;

        // Wire up event handlers
        // TODO: Re-enable once light/dark mode theme switching is reliable
        // ThemeRadioButtons.SelectionChanged += ThemeRadioButtons_SelectionChanged;
        DefaultFilterComboBox.SelectionChanged += DefaultFilterComboBox_SelectionChanged;
        DefaultSortComboBox.SelectionChanged += DefaultSortComboBox_SelectionChanged;
    }

    private void ViewModel_LanguageChanged(object? sender, EventArgs e)
    {
        _ = ShowRestartNotificationAsync();
    }

    private void LoadSettings()
    {
        // TODO: Re-enable once light/dark mode theme switching is reliable
        // ThemeRadioButtons.SelectedIndex = _viewModel.SelectedThemeIndex;

        // Load language
        var languageItem = LanguageComboBox.Items
            .OfType<LanguageInfo>()
            .FirstOrDefault(item => item.Code == _viewModel.SelectedLanguage?.Code);
        if (languageItem != null)
        {
            LanguageComboBox.SelectedItem = languageItem;
        }

        // Load default filter
        DefaultFilterComboBox.SelectedIndex = _viewModel.SelectedDefaultFilterIndex;

        // Load default sort
        DefaultSortComboBox.SelectedIndex = _viewModel.SelectedDefaultSortIndex;
    }

    private void LoadLanguages()
    {
        LanguageComboBox.ItemsSource = _viewModel.AvailableLanguages;
    }

    private void LoadVersionInfo()
    {
        var currentYear = DateTime.Now.Year;
        CopyrightText.Text = string.Format(
            GetStringOrFallback("SettingsCopyrightFormat", "© {0} Javier Ferrer. All rights reserved."),
            currentYear);
    }

    // TODO: Re-enable once light/dark mode theme switching is reliable
    // private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    // {
    //     if (_isLoading || ThemeRadioButtons.SelectedItem is not RadioButton selectedButton)
    //         return;
    //
    //     if (selectedButton.Tag is string tag)
    //     {
    //         var themeIndex = tag switch
    //         {
    //             "Light" => 0,
    //             "Dark" => 1,
    //             "Default" => 2,
    //             _ => 2
    //         };
    //
    //         _viewModel.SetThemeCommand.Execute(themeIndex);
    //     }
    // }

    private void DefaultFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || DefaultFilterComboBox.SelectedItem is not ComboBoxItem item)
            return;

        _viewModel.SetDefaultFilterCommand.Execute(DefaultFilterComboBox.SelectedIndex);
    }

    private void DefaultSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || DefaultSortComboBox.SelectedItem is not ComboBoxItem item)
            return;

        _viewModel.SetDefaultSortCommand.Execute(DefaultSortComboBox.SelectedIndex);
    }

    private async void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || LanguageComboBox.SelectedItem is not LanguageInfo languageInfo)
            return;

        // The ViewModel handles the language change and will trigger the restart dialog
        _viewModel.SelectedLanguage = languageInfo;
    }

    private async System.Threading.Tasks.Task ShowRestartNotificationAsync()
    {
        if (_isShowingRestartDialog)
            return;

        _isShowingRestartDialog = true;
        var dialog = new ContentDialog
        {
            Title = GetStringOrFallback("LanguageChangeTitle", "Language Changed"),
            Content = GetStringOrFallback("LanguageChangeMessage", "Please restart the application to apply the new language."),
            PrimaryButtonText = GetStringOrFallback("RestartNow", "Restart Now"),
            CloseButtonText = GetStringOrFallback("Later", "Later"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _viewModel.RestartWithNewLanguageAsync();
        }

        _isShowingRestartDialog = false;
    }

    private async void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = _resourceLoader.GetString("SettingsClearCacheConfirmTitle"),
            Content = _resourceLoader.GetString("SettingsClearCacheConfirmMessage"),
            PrimaryButtonText = _resourceLoader.GetString("SettingsClearCachePrimaryButton"),
            CloseButtonText = _resourceLoader.GetString("SettingsClearCacheCloseButton"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _viewModel.ClearCacheCommand.ExecuteAsync(null);

            var confirmDialog = new ContentDialog
            {
                Title = _resourceLoader.GetString("SettingsCacheClearedTitle"),
                Content = _resourceLoader.GetString("SettingsCacheClearedMessage"),
                CloseButtonText = _resourceLoader.GetString("OnboardingSignInFailedCloseButton"),
                XamlRoot = XamlRoot
            };
            await confirmDialog.ShowAsync();
        }
    }

    private async void LogOut_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = _resourceLoader.GetString("SettingsLogOutConfirmTitle"),
            Content = _resourceLoader.GetString("SettingsLogOutConfirmMessage"),
            PrimaryButtonText = _resourceLoader.GetString("SettingsLogOutPrimaryButton"),
            CloseButtonText = _resourceLoader.GetString("SettingsLogOutCloseButton"),
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _viewModel.LogOutCommand.ExecuteAsync(null);
        }
    }

    private void HamburgerButton_Click(object sender, RoutedEventArgs e)
    {
        HamburgerButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    private void ShowShortcuts_Click(object sender, RoutedEventArgs e)
    {
        ShowShortcutsRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void AboutDeveloper_Click(object sender, RoutedEventArgs e)
    {
        var content = new StackPanel
        {
            Spacing = 20,
            HorizontalAlignment = HorizontalAlignment.Center,
            MinWidth = 300
        };

        // Profile picture from GitHub
        var profileImage = new Microsoft.UI.Xaml.Controls.PersonPicture
        {
            Width = 100,
            Height = 100,
            HorizontalAlignment = HorizontalAlignment.Center,
            ProfilePicture = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("https://github.com/javierferrersb.png"))
        };
        content.Children.Add(profileImage);

        // Name
        var nameText = new TextBlock
        {
            Text = "Javier Ferrer",
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            HorizontalAlignment = HorizontalAlignment.Center,
            FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
        };
        content.Children.Add(nameText);

        // Social links
        var linksPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        // GitHub
        var githubButton = new HyperlinkButton
        {
            NavigateUri = new Uri("https://github.com/javierferrersb"),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new FontIcon { Glyph = "\uE774", FontSize = 16 },
                    new TextBlock { Text = GetStringOrFallback("GitHub", "GitHub"), VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };
        linksPanel.Children.Add(githubButton);

        // Twitter/X
        var twitterButton = new HyperlinkButton
        {
            NavigateUri = new Uri("https://x.com/javiferrer01"),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new FontIcon { Glyph = "\uE8F3", FontSize = 16 },
                    new TextBlock { Text = "X", VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };
        linksPanel.Children.Add(twitterButton);

        // LinkedIn
        var linkedinButton = new HyperlinkButton
        {
            NavigateUri = new Uri("https://linkedin.com/in/javierferrersb"),
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 6,
                Children =
                {
                    new FontIcon { Glyph = "\uE8FA", FontSize = 16 },
                    new TextBlock { Text = GetStringOrFallback("LinkedIn", "LinkedIn"), VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };
        linksPanel.Children.Add(linkedinButton);

        content.Children.Add(linksPanel);

        var dialog = new ContentDialog
        {
            Title = _resourceLoader.GetString("SettingsAboutDeveloperTitle"),
            Content = content,
            CloseButtonText = GetStringOrFallback("CloseButton", "Close"),
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }

    private string GetStringOrFallback(string key, string fallback)
    {
        var value = _resourceLoader.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    public void Dispose()
    {
        _viewModel.LanguageChanged -= ViewModel_LanguageChanged;
        _viewModel.Dispose();
    }
}
