using System;
using System.Collections.ObjectModel;
using FluentTasks.UI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentTasks.UI.Controls;

/// <summary>
/// Left sidebar navigation panel displaying user lists and action buttons.
/// </summary>
public sealed partial class NavigationPanelControl : UserControl
{
    /// <summary>
    /// User-created task list navigation items.
    /// </summary>
    public static readonly DependencyProperty UserListsProperty =
        DependencyProperty.Register(nameof(UserLists), typeof(ObservableCollection<NavItem>),
            typeof(NavigationPanelControl), new PropertyMetadata(null));

    public ObservableCollection<NavItem> UserLists
    {
        get => (ObservableCollection<NavItem>)GetValue(UserListsProperty);
        set => SetValue(UserListsProperty, value);
    }

    /// <summary>Raised when any navigation item is clicked.</summary>
    public event EventHandler<NavItem>? ItemClicked;

    /// <summary>Raised when the edit button on a user list item is clicked.</summary>
    public event EventHandler<NavItem>? EditClicked;

    /// <summary>Raised when the delete button on a user list item is clicked.</summary>
    public event EventHandler<NavItem>? DeleteClicked;

    /// <summary>Raised when the "+" create list button is clicked.</summary>
    public event EventHandler? CreateListClicked;

    /// <summary>Raised when the sync button is clicked.</summary>
    public event EventHandler? SyncClicked;

    /// <summary>Raised when the settings button is clicked.</summary>
    public event EventHandler? SettingsClicked;

    public NavigationPanelControl()
    {
        this.InitializeComponent();
    }

    private void MenuItem_Clicked(object sender, EventArgs e)
    {
        if (sender is MenuItemControl control && control.Tag is NavItem navItem)
        {
            ItemClicked?.Invoke(this, navItem);
        }
    }

    private void MenuItem_EditClicked(object sender, EventArgs e)
    {
        if (sender is MenuItemControl control && control.Tag is NavItem navItem)
        {
            EditClicked?.Invoke(this, navItem);
        }
    }

    private void MenuItem_DeleteClicked(object sender, EventArgs e)
    {
        if (sender is MenuItemControl control && control.Tag is NavItem navItem)
        {
            DeleteClicked?.Invoke(this, navItem);
        }
    }

    private void CreateList_Click(object sender, RoutedEventArgs e)
    {
        CreateListClicked?.Invoke(this, EventArgs.Empty);
    }

    private void Sync_Click(object sender, RoutedEventArgs e)
    {
        SyncClicked?.Invoke(this, EventArgs.Empty);
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        SettingsClicked?.Invoke(this, EventArgs.Empty);
    }
}
