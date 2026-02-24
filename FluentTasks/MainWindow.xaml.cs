using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using FluentTasks.Core.Models;
using FluentTasks.UI.Controls;
using FluentTasks.UI.Models;
using FluentTasks.UI.Services;
using FluentTasks.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentTasks.UI;

public sealed partial class MainWindow : Window
{
    public ShellViewModel ViewModel { get; }

    private readonly Action _onRippleRequested;
    private readonly Action<OrbStatusKind> _onOrbStatusChanged;
    private readonly Action<string> _onTemporaryStatusRequested;
    private readonly SettingsService _settingsService;
    private readonly DialogService _dialogService;
    private readonly ResourceLoader _resourceLoader;
    private bool _shouldShowTeachingTips;

    public MainWindow()
    {
        this.InitializeComponent();

        _resourceLoader = new ResourceLoader();
        ViewModel = App.GetService<ShellViewModel>();
        _settingsService = App.GetService<SettingsService>();
        _dialogService = App.GetService<DialogService>();

        // Store handlers so they can be unsubscribed on close
        _onRippleRequested = () => TaskList.Orb.TriggerRipple();
        _onOrbStatusChanged = kind => TaskList.Orb.SetStatus(kind switch
        {
            OrbStatusKind.Connected => OrbStatus.Connected,
            OrbStatusKind.Syncing => OrbStatus.Syncing,
            OrbStatusKind.Warning => OrbStatus.Warning,
            OrbStatusKind.Offline => OrbStatus.Offline,
            _ => OrbStatus.Connected
        });
        _onTemporaryStatusRequested = TaskList.ShowTemporaryStatus;

        // Wire ViewModel events to the TaskList control
        ViewModel.RippleRequested += _onRippleRequested;
        ViewModel.OrbStatusChanged += _onOrbStatusChanged;
        ViewModel.TemporaryStatusRequested += _onTemporaryStatusRequested;

        // Sync shell-level property changes to child controls
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Wire navigation panel events to the ViewModel
        NavigationPanel.ItemClicked += OnItemClicked;
        NavigationPanel.EditClicked += (_, navItem) => _ = ViewModel.RenameListAsync(navItem);
        NavigationPanel.DeleteClicked += (_, navItem) => _ = ViewModel.DeleteListAsync(navItem);
        NavigationPanel.CreateListClicked += async (_, _) => await ViewModel.CreateListCommand.ExecuteAsync(null);
        NavigationPanel.SyncClicked += async (_, _) => await ViewModel.SyncCommand.ExecuteAsync(null);
        NavigationPanel.SettingsClicked += OnSettingsClicked;

        // Wire overlay navigation panel events (for narrow screens)
        OverlayNavigationPanel.ItemClicked += OnItemClicked;
        OverlayNavigationPanel.EditClicked += (_, navItem) => _ = ViewModel.RenameListAsync(navItem);
        OverlayNavigationPanel.DeleteClicked += (_, navItem) => _ = ViewModel.DeleteListAsync(navItem);
        OverlayNavigationPanel.CreateListClicked += async (_, _) => await ViewModel.CreateListCommand.ExecuteAsync(null);
        OverlayNavigationPanel.SyncClicked += async (_, _) => await ViewModel.SyncCommand.ExecuteAsync(null);
        OverlayNavigationPanel.SettingsClicked += OnSettingsClicked;

        // Initialize auto-sync
        ViewModel.InitializeAutoSync();

        // Clean up when window closes
        this.Closed += MainWindow_Closed;

        // Set up custom title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        // Set window title (appears in taskbar) and icon
        Title = _resourceLoader.GetString("AppWindowTitle");
        this.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));

        // Initialize title bar button colors for current theme
        UpdateTitleBarButtonColors();

        // Initialize the dialog service with the XamlRoot once content is loaded
        if (this.Content is FrameworkElement root)
        {
            root.Loaded += (_, _) =>
            {
                _dialogService.SetXamlRoot(root.XamlRoot);

                // Set initial theme for dialogs
                _dialogService.SetTheme(_settingsService.AppTheme);

                // Apply initial responsive state
                UpdateResponsiveLayout();
            };

            root.SizeChanged += (_, _) => UpdateResponsiveLayout();
        }

        // Subscribe to theme changes
        _settingsService.ThemeChanged += OnThemeChanged;

        // Subscribe to logout event
        _settingsService.LoggedOut += OnLoggedOut;

        // Check if we should show teaching tips (first launch after onboarding)
        _shouldShowTeachingTips = !_settingsService.HasSeenTeachingTips;
    }

    private void UpdateResponsiveLayout()
    {
        if (this.Content is not FrameworkElement root)
            return;

        var width = root.ActualWidth;

        // Wide: >= 900px - full sidebar (280px)
        // Medium: >= 600px - compact icon-only sidebar (56px)
        // Narrow: < 600px - hidden sidebar with hamburger (0px)
        bool showHamburger;
        if (width >= 900)
        {
            NavPanelColumn.Width = new GridLength(280);
            NavigationPanel.IsCompact = false;
            NavigationPanel.Visibility = Visibility.Visible;
            showHamburger = false;
            TaskList.Padding = new Thickness(24, 16, 24, 16);
        }
        else if (width >= 600)
        {
            NavPanelColumn.Width = new GridLength(56);
            NavigationPanel.IsCompact = true;
            NavigationPanel.Visibility = Visibility.Visible;
            showHamburger = false;
            TaskList.Padding = new Thickness(16, 12, 16, 12);
        }
        else
        {
            NavPanelColumn.Width = new GridLength(0);
            NavigationPanel.Visibility = Visibility.Collapsed;
            showHamburger = true;
            TaskList.Padding = new Thickness(12, 8, 12, 8);
        }

        TaskList.ShowHamburgerButton = showHamburger;

        // Also update SettingsDialog if it's showing
        if (SettingsContainer.Content is Dialogs.SettingsDialog settingsDialog)
        {
            settingsDialog.ShowHamburgerButton = showHamburger;
        }
    }

    private void TaskList_HamburgerButtonClicked(object? sender, EventArgs e)
    {
        NavPanelOverlay.Visibility = NavPanelOverlay.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShellViewModel.IsNoListSelected):
                if (ViewModel.IsNoListSelected)
                    TaskList.ShowEmptyPlaceholder();
                break;
            case nameof(ShellViewModel.IsTaskContentVisible):
                if (ViewModel.IsTaskContentVisible)
                    TaskList.ShowTaskContent();
                break;
            case nameof(ShellViewModel.StatusText):
                TaskList.SetStatusText(ViewModel.StatusText);
                break;
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        ViewModel.StopAutoSync();

        ViewModel.RippleRequested -= _onRippleRequested;
        ViewModel.OrbStatusChanged -= _onOrbStatusChanged;
        ViewModel.TemporaryStatusRequested -= _onTemporaryStatusRequested;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

        _settingsService.ThemeChanged -= OnThemeChanged;
        _settingsService.LoggedOut -= OnLoggedOut;

        if (this.Content is FrameworkElement root)
        {
            root.ActualThemeChanged -= RootElement_ActualThemeChanged;
        }
    }

    private void RootElement_ActualThemeChanged(FrameworkElement sender, object args)
    {
        // When Windows system theme changes, update the dialog service and title bar
        _dialogService.SetTheme(sender.ActualTheme);
        UpdateTitleBarButtonColors();

        // No need to refresh task list - theme resources will update automatically
    }

    private void OnThemeChanged(object? sender, ElementTheme newTheme)
    {
        // Update theme for dialogs
        _dialogService.SetTheme(newTheme);

        // Update title bar button colors
        UpdateTitleBarButtonColors();

        // Apply theme immediately - ThemeResources will update automatically
        if (this.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = newTheme;
        }
    }

    private void OnLoggedOut(object? sender, EventArgs e)
    {
        // Restart the application by closing this window and opening a new one
        // This will trigger re-authentication when the app re-initializes
        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
    }

    private void UpdateTitleBarButtonColors()
    {
        // Determine the actual theme being used
        var actualTheme = ElementTheme.Default;
        if (this.Content is FrameworkElement rootElement)
        {
            actualTheme = rootElement.ActualTheme;
        }

        var titleBar = this.AppWindow.TitleBar;

        // Reset colors to defaults based on theme
        if (actualTheme == ElementTheme.Light)
        {
            // Light theme colors
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonPressedForegroundColor = Colors.Black;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = null; // Use system default
            titleBar.ButtonPressedBackgroundColor = null; // Use system default
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
        else
        {
            // Dark theme colors
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonPressedForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;

            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonHoverBackgroundColor = null; // Use system default
            titleBar.ButtonPressedBackgroundColor = null; // Use system default
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        }
    }

    private void OnItemClicked(object? sender, NavItem navItem)
    {
        // Hide settings and show task list
        SettingsContainer.Content = null;
        SettingsContainer.Visibility = Visibility.Collapsed;
        TaskList.Visibility = Visibility.Visible;
        NavigationPanel.IsSettingsSelected = false;

        // Close overlay nav panel if open
        HideNavOverlay();

        // Navigate to the selected list
        _ = ViewModel.SelectNavItemAsync(navItem);
    }

    private void OnSettingsClicked(object? sender, EventArgs e)
    {
        var settingsControl = new Dialogs.SettingsDialog(_settingsService);

        // Show hamburger button in settings if we're in narrow mode
        settingsControl.ShowHamburgerButton = TaskList.ShowHamburgerButton;
        settingsControl.HamburgerButtonClicked += (_, _) =>
        {
            NavPanelOverlay.Visibility = NavPanelOverlay.Visibility == Visibility.Visible
                ? Visibility.Collapsed
                : Visibility.Visible;
        };

        SettingsContainer.Content = settingsControl;
        SettingsContainer.Visibility = Visibility.Visible;
        TaskList.Visibility = Visibility.Collapsed;
        NavigationPanel.IsSettingsSelected = true;

        // Close overlay nav panel if open
        HideNavOverlay();

        // Deselect all task lists
        if (ViewModel.UserLists != null)
        {
            foreach (var navItem in ViewModel.UserLists)
            {
                navItem.IsSelected = false;
            }
        }
    }

    private void NavOverlayDismiss_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        HideNavOverlay();
    }

    private void HideNavOverlay()
    {
        NavPanelOverlay.Visibility = Visibility.Collapsed;
    }

    #region Teaching Tips

    /// <summary>
    /// Starts the teaching tips tour if the user hasn't seen it yet.
    /// </summary>
    public void StartTeachingTipsTourIfNeeded()
    {
        if (_shouldShowTeachingTips)
        {
            WelcomeTip.IsOpen = true;
        }
    }

    private void WelcomeTip_ActionButtonClick(TeachingTip sender, object args)
    {
        WelcomeTip.IsOpen = false;
        NavigationTip.IsOpen = true;
    }

    private void NavigationTip_ActionButtonClick(TeachingTip sender, object args)
    {
        NavigationTip.IsOpen = false;
        TaskListTip.IsOpen = true;
    }

    private void TaskListTip_ActionButtonClick(TeachingTip sender, object args)
    {
        TaskListTip.IsOpen = false;
        SyncTip.IsOpen = true;
    }

    private void SyncTip_ActionButtonClick(TeachingTip sender, object args)
    {
        SyncTip.IsOpen = false;
        FinishTeachingTipsTour();
    }

    private void TeachingTip_SkipTour(TeachingTip sender, object args)
    {
        // Close all tips
        WelcomeTip.IsOpen = false;
        NavigationTip.IsOpen = false;
        TaskListTip.IsOpen = false;
        SyncTip.IsOpen = false;
        FinishTeachingTipsTour();
    }

    private void FinishTeachingTipsTour()
    {
        _settingsService.HasSeenTeachingTips = true;
        _shouldShowTeachingTips = false;
    }

    #endregion
}
