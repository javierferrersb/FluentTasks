using System;
using FluentTasks.Infrastructure.Google;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentTasks.UI.Dialogs;

/// <summary>
/// Onboarding dialog that welcomes new users and handles Google sign-in.
/// </summary>
public sealed partial class OnboardingDialog : UserControl
{
    private readonly IGoogleAuthService _authService;

    /// <summary>
    /// Raised when onboarding is completed successfully.
    /// </summary>
    public event EventHandler? OnboardingCompleted;

    public OnboardingDialog(IGoogleAuthService authService)
    {
        ArgumentNullException.ThrowIfNull(authService);
        _authService = authService;
        InitializeComponent();
    }

    private void OnboardingFlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var index = OnboardingFlipView.SelectedIndex;

        // Update page indicators
        if (index == 0)
        {
            Page1Indicator.Fill = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
            Page2IndicatorWelcome.Fill = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ControlStrongFillColorDefaultBrush"];
            Page2IndicatorWelcome.Opacity = 0.4;
        }
        else
        {
            Page1IndicatorSignIn.Fill = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["ControlStrongFillColorDefaultBrush"];
            Page1IndicatorSignIn.Opacity = 0.4;
            Page2Indicator.Fill = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        }
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        OnboardingFlipView.SelectedIndex = 1;
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        OnboardingFlipView.SelectedIndex = 0;
    }

    private async void GoogleSignInButton_Click(object sender, RoutedEventArgs e)
    {
        // Show loading state
        GoogleSignInButton.Visibility = Visibility.Collapsed;
        SigningInPanel.Visibility = Visibility.Visible;

        try
        {
            // Attempt to get credentials (this will trigger the OAuth flow)
            await _authService.GetCredentialAsync();

            // Sign-in successful - notify completion
            OnboardingCompleted?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception)
        {
            // Sign-in failed or was cancelled - restore button
            GoogleSignInButton.Visibility = Visibility.Visible;
            SigningInPanel.Visibility = Visibility.Collapsed;

            // Show error dialog
            var dialog = new ContentDialog
            {
                Title = "Sign-in failed",
                Content = "We couldn't sign you in. Please try again.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
