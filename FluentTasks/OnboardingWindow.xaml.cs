using System;
using System.IO;
using FluentTasks.Infrastructure.Google;
using FluentTasks.UI.Dialogs;
using FluentTasks.UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentTasks.UI;

/// <summary>
/// Window that hosts the onboarding experience for new users.
/// </summary>
public sealed partial class OnboardingWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly ResourceLoader _resourceLoader;

    /// <summary>
    /// Raised when onboarding is completed and the main app should start.
    /// </summary>
    public event EventHandler? OnboardingCompleted;

    public OnboardingWindow()
    {
        InitializeComponent();

        _resourceLoader = new ResourceLoader();
        _settingsService = App.GetService<SettingsService>();

        // Set up custom title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        // Set window title and icon
        Title = _resourceLoader.GetString("OnboardingWindowTitle");
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico"));

        // Initialize settings and theme
        _settingsService.SetMainWindow(this);
        _settingsService.InitializeTheme();

        // Create and set up the onboarding dialog
        var authService = App.GetService<IGoogleAuthService>();
        var onboardingDialog = new OnboardingDialog(authService);
        onboardingDialog.OnboardingCompleted += OnOnboardingCompleted;

        OnboardingContent.Content = onboardingDialog;

        // Apply theme
        if (Content is FrameworkElement root)
        {
            root.RequestedTheme = _settingsService.AppTheme;
        }
    }

    private void OnOnboardingCompleted(object? sender, EventArgs e)
    {
        // Mark onboarding as completed
        _settingsService.HasCompletedOnboarding = true;

        // Notify App.xaml.cs to show the main window
        OnboardingCompleted?.Invoke(this, EventArgs.Empty);
    }
}
