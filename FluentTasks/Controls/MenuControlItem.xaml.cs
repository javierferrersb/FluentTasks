using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;

namespace FluentTasks.UI.Controls;

public sealed partial class MenuItemControl : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(string), typeof(MenuItemControl), new PropertyMetadata("\uE8F4"));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(MenuItemControl), new PropertyMetadata(""));

    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(MenuItemControl), new PropertyMetadata(false, OnIsSelectedChanged));

    public static readonly DependencyProperty ShowActionsProperty =
        DependencyProperty.Register(nameof(ShowActions), typeof(bool), typeof(MenuItemControl), new PropertyMetadata(false));

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public bool ShowActions
    {
        get => (bool)GetValue(ShowActionsProperty);
        set => SetValue(ShowActionsProperty, value);
    }

    public event EventHandler? ItemClicked;
    public event EventHandler? EditClicked;
    public event EventHandler? DeleteClicked;

    public MenuItemControl()
    {
        this.InitializeComponent();
        UpdateButtonStyle();

        RootButton.PointerEntered += (_, _) =>
        {
            if (ShowActions)
            {
                EditButton.Visibility = Visibility.Visible;
                DeleteButton.Visibility = Visibility.Visible;
            }
        };
        RootButton.PointerExited += (_, _) =>
        {
            EditButton.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
        };
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuItemControl control)
        {
            control.UpdateButtonStyle();
        }
    }

    private void UpdateButtonStyle()
    {
        var styleName = IsSelected ? "SelectedStyle" : "UnselectedStyle";
        RootButton.Style = (Style)Resources[styleName];
    }

    private void RootButton_Click(object sender, RoutedEventArgs e)
    {
        ItemClicked?.Invoke(this, EventArgs.Empty);
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        EditClicked?.Invoke(this, EventArgs.Empty);
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        DeleteClicked?.Invoke(this, EventArgs.Empty);
    }
}