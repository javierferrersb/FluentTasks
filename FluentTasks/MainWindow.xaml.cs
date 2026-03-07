using FluentTasks.UI.Controls;
using FluentTasks.UI.Models;
using FluentTasks.UI.Services;
using FluentTasks.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.ComponentModel;
using System.IO;

namespace FluentTasks.UI;

public sealed partial class MainWindow : Window
{
    public ShellViewModel ViewModel { get; }

    private readonly SettingsService _settingsService;
    private readonly DialogService _dialogService;
    private readonly ResourceLoader _resourceLoader;
    private bool _shouldShowTeachingTips;

    // Event handler references for cleanup
    private readonly Action _onRippleRequested;
    private readonly Action<OrbStatusKind> _onOrbStatusChanged;
    private readonly Action<string> _onTemporaryStatusRequested;

    public MainWindow()
    {
        InitializeComponent();

        _resourceLoader = new ResourceLoader();
        ViewModel = App.GetService<ShellViewModel>();
        _settingsService = App.GetService<SettingsService>();
        _dialogService = App.GetService<DialogService>();

        // Store handlers for cleanup on close
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

        // Wire ViewModel events to UI
        ViewModel.RippleRequested += _onRippleRequested;
        ViewModel.OrbStatusChanged += _onOrbStatusChanged;
        ViewModel.TemporaryStatusRequested += _onTemporaryStatusRequested;
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Initialize auto-sync
        ViewModel.InitializeAutoSync();

        // Set up custom title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        // Set window title and icon
        Title = _resourceLoader.GetString("AppWindowTitle");
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));

        // Initialize title bar button colors for current theme
        UpdateTitleBarButtonColors();

        // Initialize dialog service once content is loaded
        if (Content is FrameworkElement root)
        {
            root.Loaded += (_, _) =>
            {
                _dialogService.SetXamlRoot(root.XamlRoot);
                _dialogService.SetTheme(_settingsService.AppTheme);
                UpdateResponsiveLayout();
            };

            root.SizeChanged += (_, _) => UpdateResponsiveLayout();
        }

        // Subscribe to theme and logout events
        _settingsService.ThemeChanged += OnThemeChanged;
        _settingsService.LoggedOut += OnLoggedOut;

        // Check if we should show teaching tips
        _shouldShowTeachingTips = !_settingsService.HasSeenTeachingTips;

        // Clean up when window closes
        Closed += MainWindow_Closed;
    }

    private void UpdateResponsiveLayout()
    {
        if (Content is not FrameworkElement root)
            return;

        var width = root.ActualWidth;
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

        // Update SettingsDialog if showing
        if (SettingsContainer.Content is Dialogs.SettingsDialog settingsDialog)
        {
            settingsDialog.ShowHamburgerButton = showHamburger;
        }
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

        // Unsubscribe from ViewModel events
        ViewModel.RippleRequested -= _onRippleRequested;
        ViewModel.OrbStatusChanged -= _onOrbStatusChanged;
        ViewModel.TemporaryStatusRequested -= _onTemporaryStatusRequested;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;

        // Unsubscribe from settings events
        _settingsService.ThemeChanged -= OnThemeChanged;
        _settingsService.LoggedOut -= OnLoggedOut;

        if (Content is FrameworkElement root)
        {
            root.ActualThemeChanged -= RootElement_ActualThemeChanged;
        }
    }

    private void RootElement_ActualThemeChanged(FrameworkElement sender, object args)
    {
        _dialogService.SetTheme(sender.ActualTheme);
        UpdateTitleBarButtonColors();
    }

    private void OnThemeChanged(object? sender, ElementTheme newTheme)
    {
        _dialogService.SetTheme(newTheme);
        UpdateTitleBarButtonColors();

        if (Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = newTheme;
        }
    }

    private void OnLoggedOut(object? sender, EventArgs e)
    {
        Microsoft.Windows.AppLifecycle.AppInstance.Restart("");
    }

    private void UpdateTitleBarButtonColors()
    {
        var actualTheme = Content is FrameworkElement rootElement
            ? rootElement.ActualTheme
            : ElementTheme.Default;

        var titleBar = AppWindow.TitleBar;

        if (actualTheme == ElementTheme.Light)
        {
            titleBar.ButtonForegroundColor = Colors.Black;
            titleBar.ButtonHoverForegroundColor = Colors.Black;
            titleBar.ButtonPressedForegroundColor = Colors.Black;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
        }
        else
        {
            titleBar.ButtonForegroundColor = Colors.White;
            titleBar.ButtonHoverForegroundColor = Colors.White;
            titleBar.ButtonPressedForegroundColor = Colors.White;
            titleBar.ButtonInactiveForegroundColor = Colors.Gray;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
        }
    }

    #region Navigation Panel Event Handlers

    private void NavigationPanel_ItemClicked(object? sender, NavItem navItem) => OnItemClicked(navItem);
    private void OverlayNavigationPanel_ItemClicked(object? sender, NavItem navItem) => OnItemClicked(navItem);

    private void NavigationPanel_EditClicked(object? sender, NavItem navItem) => _ = ViewModel.RenameListAsync(navItem);
    private void OverlayNavigationPanel_EditClicked(object? sender, NavItem navItem) => _ = ViewModel.RenameListAsync(navItem);

    private void NavigationPanel_DeleteClicked(object? sender, NavItem navItem) => _ = ViewModel.DeleteListAsync(navItem);
    private void OverlayNavigationPanel_DeleteClicked(object? sender, NavItem navItem) => _ = ViewModel.DeleteListAsync(navItem);

    private async void NavigationPanel_CreateListClicked(object? sender, EventArgs e) => await ViewModel.CreateListCommand.ExecuteAsync(null);
    private async void OverlayNavigationPanel_CreateListClicked(object? sender, EventArgs e) => await ViewModel.CreateListCommand.ExecuteAsync(null);

    private async void NavigationPanel_SyncClicked(object? sender, EventArgs e) => await ViewModel.SyncCommand.ExecuteAsync(null);
    private async void OverlayNavigationPanel_SyncClicked(object? sender, EventArgs e) => await ViewModel.SyncCommand.ExecuteAsync(null);

    private void NavigationPanel_SettingsClicked(object? sender, EventArgs e) => OnSettingsClicked();
    private void OverlayNavigationPanel_SettingsClicked(object? sender, EventArgs e) => OnSettingsClicked();

    #endregion

    private void OnItemClicked(NavItem navItem)
    {
        HideSettingsAndShowTaskList();
        HideNavOverlay();
        _ = ViewModel.SelectNavItemAsync(navItem);
    }

    private void OnSettingsClicked()
    {
        var viewModel = App.GetService<SettingsViewModel>();
        var settingsControl = new Dialogs.SettingsDialog(viewModel)
        {
            ShowHamburgerButton = TaskList.ShowHamburgerButton
        };

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

        HideNavOverlay();
        DeselectAllTaskLists();
    }

    private void HideSettingsAndShowTaskList()
    {
        SettingsContainer.Content = null;
        SettingsContainer.Visibility = Visibility.Collapsed;
        TaskList.Visibility = Visibility.Visible;
        NavigationPanel.IsSettingsSelected = false;
    }

    private void DeselectAllTaskLists()
    {
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

    private void TaskList_HamburgerButtonClicked(object? sender, EventArgs e)
    {
        NavPanelOverlay.Visibility = NavPanelOverlay.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    #region Teaching Tips

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
