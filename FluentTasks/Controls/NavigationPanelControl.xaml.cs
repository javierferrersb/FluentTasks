using System;
using System.Collections.ObjectModel;
using FluentTasks.UI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentTasks.UI.Controls;

/// <summary>
/// Left sidebar navigation panel displaying user lists and action buttons.
/// </summary>
public sealed partial class NavigationPanelControl : UserControl
{
    private readonly ResourceLoader _resourceLoader = new();

    /// <summary>
    /// User-created task list navigation items.
    /// </summary>
    public static readonly DependencyProperty UserListsProperty =
        DependencyProperty.Register(nameof(UserLists), typeof(ObservableCollection<NavItem>),
            typeof(NavigationPanelControl), new PropertyMetadata(null));

    public static readonly DependencyProperty IsSettingsSelectedProperty =
        DependencyProperty.Register(nameof(IsSettingsSelected), typeof(bool),
            typeof(NavigationPanelControl), new PropertyMetadata(false, OnIsSettingsSelectedChanged));

    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(nameof(IsCompact), typeof(bool),
            typeof(NavigationPanelControl), new PropertyMetadata(false, OnIsCompactChanged));

    public ObservableCollection<NavItem> UserLists
    {
        get => (ObservableCollection<NavItem>)GetValue(UserListsProperty);
        set => SetValue(UserListsProperty, value);
    }

    public bool IsSettingsSelected
    {
        get => (bool)GetValue(IsSettingsSelectedProperty);
        set => SetValue(IsSettingsSelectedProperty, value);
    }

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
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

    private static void OnIsSettingsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationPanelControl control)
        {
            control.UpdateSettingsButtonStyle();
        }
    }

    private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is NavigationPanelControl control)
        {
            control.UpdateCompactMode();
        }
    }

    private void UpdateCompactMode()
    {
        if (IsCompact)
        {
            MyListsText.Visibility = Visibility.Collapsed;
            CreateListButton.Visibility = Visibility.Visible;
            SettingsButtonText.Visibility = Visibility.Collapsed;
            SyncButtonText.Visibility = Visibility.Collapsed;
            SettingsButton.HorizontalContentAlignment = HorizontalAlignment.Center;
            SyncButton.HorizontalContentAlignment = HorizontalAlignment.Center;
            SettingsButton.Padding = new Thickness(8, 12, 8, 12);
            SyncButton.Padding = new Thickness(8, 12, 8, 12);
            NavContentGrid.Padding = new Thickness(4, 8, 4, 8);
            CreateListButton.HorizontalAlignment = HorizontalAlignment.Center;
            CreateListButton.Padding = new Thickness(8, 10, 8, 10);

            // Center the button in the grid by adjusting column widths
            var col0 = MyListsHeader.ColumnDefinitions[0];
            var col1 = MyListsHeader.ColumnDefinitions[1];
            col0.Width = new GridLength(1, GridUnitType.Star);
            col1.Width = new GridLength(1, GridUnitType.Star);

            ToolTipService.SetToolTip(SettingsButton, GetResource("SettingsButtonTextBlock/Text", "Settings"));
            ToolTipService.SetToolTip(SyncButton, GetResource("SyncButtonTextBlock/Text", "Sync Lists"));
            ToolTipService.SetToolTip(CreateListButton, GetResource("CreateListButton/ToolTipService/ToolTip", "Create new list"));
        }
        else
        {
            MyListsText.Visibility = Visibility.Visible;
            CreateListButton.Visibility = Visibility.Visible;
            SettingsButtonText.Visibility = Visibility.Visible;
            SyncButtonText.Visibility = Visibility.Visible;
            SettingsButton.HorizontalContentAlignment = HorizontalAlignment.Left;
            SyncButton.HorizontalContentAlignment = HorizontalAlignment.Left;
            SettingsButton.Padding = new Thickness(20, 12, 20, 12);
            SyncButton.Padding = new Thickness(20, 12, 20, 12);
            NavContentGrid.Padding = new Thickness(8, 2, 8, 8);
            CreateListButton.HorizontalAlignment = HorizontalAlignment.Right;
            CreateListButton.Padding = new Thickness(4, 4, 4, 4);

            // Restore normal column widths
            var col0 = MyListsHeader.ColumnDefinitions[0];
            var col1 = MyListsHeader.ColumnDefinitions[1];
            col0.Width = new GridLength(1, GridUnitType.Star);
            col1.Width = GridLength.Auto;

            ToolTipService.SetToolTip(SettingsButton, null);
            ToolTipService.SetToolTip(SyncButton, null);
            ToolTipService.SetToolTip(CreateListButton, null);
        }

        // Update all menu item controls
        UpdateMenuItemsCompactMode();
    }

    private void UpdateMenuItemsCompactMode()
    {
        // Find all MenuItemControl instances in the visual tree and update them
        UpdateMenuItemsInVisualTree(UserListsView);
    }

    private void UpdateMenuItemsInVisualTree(DependencyObject parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is MenuItemControl menuItem)
            {
                menuItem.IsCompact = IsCompact;
            }
            else
            {
                UpdateMenuItemsInVisualTree(child);
            }
        }
    }

    private void UpdateSettingsButtonStyle()
    {
        SettingsButton.Style = IsSettingsSelected
            ? (Style)Application.Current.Resources["AccentButtonStyle"]
            : (Style)Application.Current.Resources["SubtleButtonStyle"];
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

    private void MenuItem_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItemControl menuItem)
        {
            menuItem.IsCompact = IsCompact;
        }
    }

    private string GetResource(string key, string fallback)
    {
        var value = _resourceLoader.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
