using System;
using System.ComponentModel;
using FluentTasks.UI.Controls;
using FluentTasks.UI.Models;
using FluentTasks.UI.Services;
using FluentTasks.UI.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace FluentTasks.UI;

public sealed partial class MainWindow : Window
{
    public ShellViewModel ViewModel { get; }

    private readonly Action _onRippleRequested;
    private readonly Action<OrbStatusKind> _onOrbStatusChanged;
    private readonly Action<string> _onTemporaryStatusRequested;

    public MainWindow()
    {
        this.InitializeComponent();

        ViewModel = App.GetService<ShellViewModel>();

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
        NavigationPanel.ItemClicked += (_, navItem) => _ = ViewModel.SelectNavItemAsync(navItem);
        NavigationPanel.EditClicked += (_, navItem) => _ = ViewModel.RenameListAsync(navItem);
        NavigationPanel.DeleteClicked += (_, navItem) => _ = ViewModel.DeleteListAsync(navItem);
        NavigationPanel.CreateListClicked += async (_, _) => await ViewModel.CreateListCommand.ExecuteAsync(null);
        NavigationPanel.SyncClicked += async (_, _) => await ViewModel.SyncCommand.ExecuteAsync(null);

        // Initialize auto-sync
        ViewModel.InitializeAutoSync();

        // Clean up when window closes
        this.Closed += MainWindow_Closed;

        // Set up custom title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        // Initialize the dialog service with the XamlRoot once content is loaded
        if (this.Content is FrameworkElement root)
        {
            root.Loaded += (_, _) =>
            {
                var dialogService = App.GetService<DialogService>();
                dialogService.SetXamlRoot(root.XamlRoot);
            };
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

        ViewModel.RippleRequested -= _onRippleRequested;
        ViewModel.OrbStatusChanged -= _onOrbStatusChanged;
        ViewModel.TemporaryStatusRequested -= _onTemporaryStatusRequested;
        ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
    }
}
