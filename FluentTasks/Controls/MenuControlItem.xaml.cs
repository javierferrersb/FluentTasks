using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

    public static readonly DependencyProperty IsCompactProperty =
        DependencyProperty.Register(nameof(IsCompact), typeof(bool), typeof(MenuItemControl), new PropertyMetadata(false, OnIsCompactChanged));

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

    public bool IsCompact
    {
        get => (bool)GetValue(IsCompactProperty);
        set => SetValue(IsCompactProperty, value);
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
            if (ShowActions && !IsCompact)
            {
                EditButton.Opacity = 1;
                EditButton.IsHitTestVisible = true;
                DeleteButton.Opacity = 1;
                DeleteButton.IsHitTestVisible = true;
            }
        };
        RootButton.PointerExited += (_, _) =>
        {
            EditButton.Opacity = 0;
            EditButton.IsHitTestVisible = false;
            DeleteButton.Opacity = 0;
            DeleteButton.IsHitTestVisible = false;
        };
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuItemControl control)
        {
            control.UpdateButtonStyle();
        }
    }

    private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is MenuItemControl control)
        {
            control.UpdateCompactMode();
        }
    }

    private void UpdateCompactMode()
    {
        if (IsCompact)
        {
            ItemText.Visibility = Visibility.Collapsed;
            EditButton.Visibility = Visibility.Collapsed;
            DeleteButton.Visibility = Visibility.Collapsed;
            ItemIcon.Margin = new Thickness(0);
            RootButton.HorizontalContentAlignment = HorizontalAlignment.Center;
            RootButton.Padding = new Thickness(8, 10, 8, 10);
            ToolTipService.SetToolTip(RootButton, Text);
        }
        else
        {
            ItemText.Visibility = Visibility.Visible;
            EditButton.Visibility = Visibility.Visible;
            DeleteButton.Visibility = Visibility.Visible;
            ItemIcon.Margin = new Thickness(0, 0, 12, 0);
            RootButton.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            RootButton.Padding = new Thickness(12, 8, 12, 8);
            ToolTipService.SetToolTip(RootButton, null);
        }
    }

    private void UpdateButtonStyle()
    {
        // Just switch the style - the Foreground bindings in XAML will automatically
        // pick up the correct foreground from AccentButtonStyle or SubtleButtonStyle
        RootButton.Style = IsSelected
            ? (Style)Application.Current.Resources["AccentButtonStyle"]
            : (Style)Application.Current.Resources["SubtleButtonStyle"];
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