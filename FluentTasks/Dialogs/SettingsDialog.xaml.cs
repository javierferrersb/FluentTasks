using FluentTasks.Core.Models;
using FluentTasks.UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace FluentTasks.UI.Dialogs;

/// <summary>
/// Unified settings control with all settings on a single page.
/// </summary>
public sealed partial class SettingsDialog : UserControl
{
    private readonly SettingsService _settingsService;
    private bool _isLoading;

    /// <summary>
    /// Raised when the hamburger menu button is clicked.
    /// </summary>
    public event EventHandler? HamburgerButtonClicked;

    /// <summary>
    /// Whether to show the hamburger menu button in the header.
    /// </summary>
    public bool ShowHamburgerButton
    {
        get => HamburgerButton.Visibility == Visibility.Visible;
        set => HamburgerButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
    }

    public SettingsDialog(SettingsService settingsService)
    {
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        InitializeComponent();
        LoadSettings();
        LoadVersionInfo();

        // Wire up event handlers
        ThemeRadioButtons.SelectionChanged += ThemeRadioButtons_SelectionChanged;
        DefaultFilterComboBox.SelectionChanged += DefaultFilterComboBox_SelectionChanged;
        DefaultSortComboBox.SelectionChanged += DefaultSortComboBox_SelectionChanged;
    }

    private void LoadSettings()
    {
        _isLoading = true;

        // Load theme
        var theme = _settingsService.AppTheme;
        ThemeRadioButtons.SelectedIndex = theme switch
        {
            ElementTheme.Light => 0,
            ElementTheme.Dark => 1,
            ElementTheme.Default => 2,
            _ => 2
        };

        // Load default filter
        var filterItem = DefaultFilterComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == _settingsService.DefaultFilter.ToString());
        if (filterItem != null)
        {
            DefaultFilterComboBox.SelectedItem = filterItem;
        }

        // Load default sort
        var sortItem = DefaultSortComboBox.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => item.Tag?.ToString() == _settingsService.DefaultSort.ToString());
        if (sortItem != null)
        {
            DefaultSortComboBox.SelectedItem = sortItem;
        }

        _isLoading = false;
    }

    private void LoadVersionInfo()
    {
        var currentYear = DateTime.Now.Year;
        CopyrightText.Text = $"© {currentYear} Javier Ferrer. All rights reserved.";
    }

    private void ThemeRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || ThemeRadioButtons.SelectedItem is not RadioButton selectedButton)
            return;

        if (selectedButton.Tag is string tag)
        {
            var theme = tag switch
            {
                "Light" => ElementTheme.Light,
                "Dark" => ElementTheme.Dark,
                "Default" => ElementTheme.Default,
                _ => ElementTheme.Default
            };

            _settingsService.AppTheme = theme;
        }
    }

    private void DefaultFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || DefaultFilterComboBox.SelectedItem is not ComboBoxItem item)
            return;

        if (Enum.TryParse<FilterOption>(item.Tag?.ToString(), out var filter))
        {
            _settingsService.DefaultFilter = filter;
        }
    }

    private void DefaultSortComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading || DefaultSortComboBox.SelectedItem is not ComboBoxItem item)
            return;

        if (Enum.TryParse<SortOption>(item.Tag?.ToString(), out var sort))
        {
            _settingsService.DefaultSort = sort;
        }
    }

    private async void ClearCache_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Clear cache?",
            Content = "This will remove all locally cached data. Your tasks will be re-downloaded on the next sync.",
            PrimaryButtonText = "Clear",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _settingsService.ClearCacheAsync();

            var confirmDialog = new ContentDialog
            {
                Title = "Cache cleared",
                Content = "Local cache has been cleared successfully.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await confirmDialog.ShowAsync();
        }
    }

    private async void LogOut_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Log out?",
            Content = "You'll need to sign in again to access your tasks.",
            PrimaryButtonText = "Log out",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            await _settingsService.LogOutAsync();
        }
    }

    private void HamburgerButton_Click(object sender, RoutedEventArgs e)
    {
        HamburgerButtonClicked?.Invoke(this, EventArgs.Empty);
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
                    new TextBlock { Text = "GitHub", VerticalAlignment = VerticalAlignment.Center }
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
                    new TextBlock { Text = "LinkedIn", VerticalAlignment = VerticalAlignment.Center }
                }
            }
        };
        linksPanel.Children.Add(linkedinButton);

        content.Children.Add(linksPanel);

        var dialog = new ContentDialog
        {
            Title = "About the Developer",
            Content = content,
            CloseButtonText = "Close",
            XamlRoot = XamlRoot
        };

        await dialog.ShowAsync();
    }
}
