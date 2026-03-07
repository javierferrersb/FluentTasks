using FluentTasks.UI.Models;
using FluentTasks.UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using Windows.System;

namespace FluentTasks.UI.Controls;

/// <summary>
/// Floating overlay that displays all keyboard shortcuts grouped by category.
/// Supports three trigger mechanisms: hold Ctrl (500 ms), toggle via ? or Ctrl+Shift+?,
/// and programmatic show via <see cref="ShowOverlay"/>.
/// </summary>
public sealed partial class ShortcutsOverlayControl : UserControl
{
    // Polls whether Ctrl is physically held; fires the hold timer when Ctrl stays down.
    private readonly DispatcherTimer _ctrlPollTimer;
    private readonly DispatcherTimer _ctrlHoldTimer;
    private bool _wasCtrlDown;
    private bool _isCtrlHeld;
    private bool _isToggledOpen;

    /// <summary>
    /// Whether the overlay is currently visible.
    /// </summary>
    public bool IsOpen => Visibility == Visibility.Visible;

    /// <summary>
    /// Raised when the overlay requests to be closed (e.g. Escape or backdrop click).
    /// </summary>
    public event EventHandler? CloseRequested;

    public ShortcutsOverlayControl()
    {
        InitializeComponent();

        var resources = new ResourceLoader();

        // Set category headers from resources
        NavigationCategoryHeader.Text = KeyboardShortcutRegistry.CategoryNavigation;
        TasksCategoryHeader.Text = KeyboardShortcutRegistry.CategoryTasks;
        ViewCategoryHeader.Text = KeyboardShortcutRegistry.CategoryView;
        FooterText.Text = resources.GetString("ShortcutsOverlayFooter");

        NavigationShortcuts.ItemsSource = KeyboardShortcutRegistry.GetByCategory(KeyboardShortcutRegistry.CategoryNavigation);
        TaskShortcuts.ItemsSource = KeyboardShortcutRegistry.GetByCategory(KeyboardShortcutRegistry.CategoryTasks);
        ViewShortcuts.ItemsSource = KeyboardShortcutRegistry.GetByCategory(KeyboardShortcutRegistry.CategoryView);

        _ctrlHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _ctrlHoldTimer.Tick += CtrlHoldTimer_Tick;

        // Poll the Ctrl key state every 50 ms so we reliably detect press/release
        // even when PreviewKeyDown does not fire for modifier-only keys.
        _ctrlPollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _ctrlPollTimer.Tick += CtrlPollTimer_Tick;

        Loaded += (_, _) => _ctrlPollTimer.Start();
        Unloaded += (_, _) =>
        {
            _ctrlPollTimer.Stop();
            _ctrlHoldTimer.Stop();
        };
    }

    private void CtrlPollTimer_Tick(object? sender, object e)
    {
        // Suppress the overlay while a ContentDialog is open.
        if (App.GetService<DialogService>().IsDialogOpen)
        {
            _ctrlHoldTimer.Stop();
            _wasCtrlDown = false;
            return;
        }

        bool isCtrlDown = Microsoft.UI.Input.InputKeyboardSource
            .GetKeyStateForCurrentThread(VirtualKey.Control)
            .HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);

        if (isCtrlDown && !_wasCtrlDown)
        {
            // Ctrl just pressed — start the hold timer
            OnCtrlPressed();
        }
        else if (!isCtrlDown && _wasCtrlDown)
        {
            // Ctrl just released
            OnCtrlReleased();
        }

        _wasCtrlDown = isCtrlDown;
    }

    private void OnCtrlPressed()
    {
        if (_isToggledOpen || _isCtrlHeld)
            return;

        _ctrlHoldTimer.Start();
    }

    private void OnCtrlReleased()
    {
        _ctrlHoldTimer.Stop();

        if (_isCtrlHeld)
        {
            _isCtrlHeld = false;
            HideOverlayAnimated();
        }
    }

    /// <summary>
    /// Toggles the overlay on/off (for ? and Ctrl+Shift+? triggers).
    /// </summary>
    public void ToggleOverlay()
    {
        if (_isToggledOpen)
        {
            _isToggledOpen = false;
            HideOverlayAnimated();
        }
        else
        {
            _isToggledOpen = true;
            _isCtrlHeld = false;
            _ctrlHoldTimer.Stop();
            ShowOverlayAnimated();
        }
    }

    /// <summary>
    /// Shows the overlay programmatically (e.g. from a menu button).
    /// </summary>
    public void ShowOverlay()
    {
        _isToggledOpen = true;
        _isCtrlHeld = false;
        _ctrlHoldTimer.Stop();
        ShowOverlayAnimated();
    }

    /// <summary>
    /// Hides the overlay unconditionally.
    /// </summary>
    public void HideOverlay()
    {
        _isToggledOpen = false;
        _isCtrlHeld = false;
        _ctrlHoldTimer.Stop();
        HideOverlayAnimated();
    }

    private void CtrlHoldTimer_Tick(object? sender, object e)
    {
        _ctrlHoldTimer.Stop();
        _isCtrlHeld = true;
        ShowOverlayAnimated();
    }

    private void ShowOverlayAnimated()
    {
        if (Visibility == Visibility.Visible)
            return;

        Visibility = Visibility.Visible;

        // Fade in backdrop
        var backdropFade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(backdropFade, Backdrop);
        Storyboard.SetTargetProperty(backdropFade, "Opacity");

        // Fade in panel
        var panelFade = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(panelFade, OverlayPanel);
        Storyboard.SetTargetProperty(panelFade, "Opacity");

        var sb = new Storyboard();
        sb.Children.Add(backdropFade);
        sb.Children.Add(panelFade);
        sb.Begin();

        // Slide up via Translation (compositor-driven)
        OverlayPanel.Translation = new System.Numerics.Vector3(0, 0, 0);
    }

    private void HideOverlayAnimated()
    {
        if (Visibility != Visibility.Visible)
            return;

        var backdropFade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(backdropFade, Backdrop);
        Storyboard.SetTargetProperty(backdropFade, "Opacity");

        var panelFade = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(150)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(panelFade, OverlayPanel);
        Storyboard.SetTargetProperty(panelFade, "Opacity");

        var sb = new Storyboard();
        sb.Children.Add(backdropFade);
        sb.Children.Add(panelFade);

        sb.Completed += (_, _) =>
        {
            Visibility = Visibility.Collapsed;
            OverlayPanel.Translation = new System.Numerics.Vector3(0, 20, 0);
        };

        sb.Begin();
    }

    private void Backdrop_Tapped(object sender, TappedRoutedEventArgs e)
    {
        HideOverlay();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        HideOverlay();
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
