using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using Windows.UI;

namespace FluentTasks.UI.Controls;

public enum OrbStatus
{
    Connected,
    Syncing,
    Warning,
    Offline
}

public sealed partial class StatusOrb : UserControl
{
    private Storyboard? _breathingAnimation;

    public StatusOrb()
    {
        this.InitializeComponent();
        SetStatus(OrbStatus.Connected);
    }

    public void SetStatus(OrbStatus status)
    {
        StopAnimation();

        switch (status)
        {
            case OrbStatus.Connected:
                OrbEllipse.Stroke = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129)); // Green
                StartBreathingAnimation(2500); // Slower breathing (2.5s)
                break;

            case OrbStatus.Syncing:
                OrbEllipse.Stroke = new SolidColorBrush(Color.FromArgb(255, 59, 130, 246)); // Blue
                StartBreathingAnimation(800); // Fast pulse
                break;

            case OrbStatus.Warning:
                OrbEllipse.Stroke = new SolidColorBrush(Color.FromArgb(255, 251, 146, 60)); // Orange
                StartBreathingAnimation(1000); // Medium pulse
                break;

            case OrbStatus.Offline:
                OrbEllipse.Stroke = new SolidColorBrush(Color.FromArgb(255, 239, 68, 68)); // Red
                break;
        }
    }

    private void StartBreathingAnimation(int durationMs)
    {
        _breathingAnimation = new Storyboard();
        _breathingAnimation.RepeatBehavior = RepeatBehavior.Forever;

        var scaleAnimation = new DoubleAnimationUsingKeyFrames();
        scaleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
            Value = 1.0
        });
        scaleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs / 2)),
            Value = 1.3,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        scaleAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)),
            Value = 1.0,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });

        Storyboard.SetTarget(scaleAnimation, OrbScale);
        Storyboard.SetTargetProperty(scaleAnimation, "ScaleX");
        _breathingAnimation.Children.Add(scaleAnimation);

        var scaleYAnimation = new DoubleAnimationUsingKeyFrames();
        scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
            Value = 1.0
        });
        scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs / 2)),
            Value = 1.3,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        scaleYAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)),
            Value = 1.0,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });

        Storyboard.SetTarget(scaleYAnimation, OrbScale);
        Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");
        _breathingAnimation.Children.Add(scaleYAnimation);

        var opacityAnimation = new DoubleAnimationUsingKeyFrames();
        opacityAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.Zero),
            Value = 0.8
        });
        opacityAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs / 2)),
            Value = 1.0,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });
        opacityAnimation.KeyFrames.Add(new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(durationMs)),
            Value = 0.8,
            EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
        });

        Storyboard.SetTarget(opacityAnimation, OrbEllipse);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");
        _breathingAnimation.Children.Add(opacityAnimation);

        _breathingAnimation.Begin();
    }

    private void StopAnimation()
    {
        _breathingAnimation?.Stop();
        _breathingAnimation = null;

        OrbScale.ScaleX = 1.0;
        OrbScale.ScaleY = 1.0;
        OrbEllipse.Opacity = 1.0;
    }

    /// <summary>
    /// Triggers a ripple effect - a ring that expands from center to orb size and fades
    /// </summary>
    public void TriggerRipple()
    {
        // Set ripple color to match main orb
        RippleEllipse.Stroke = OrbEllipse.Stroke;

        var rippleStoryboard = new Storyboard();

        // Scale animation - grow from 0 (center) to 1.0 (orb size)
        var scaleXAnimation = new DoubleAnimation
        {
            From = 0.0,  // Start from center
            To = 1.4,    // End at orb size
            Duration = new Duration(TimeSpan.FromMilliseconds(600)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        var scaleYAnimation = new DoubleAnimation
        {
            From = 0.0,  // Start from center
            To = 1.4,    // End at orb size
            Duration = new Duration(TimeSpan.FromMilliseconds(600)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        // Opacity animation - fade from 1.0 to 0
        var opacityAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = 0.0,
            Duration = new Duration(TimeSpan.FromMilliseconds(600)),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };

        Storyboard.SetTarget(scaleXAnimation, RippleScale);
        Storyboard.SetTargetProperty(scaleXAnimation, "ScaleX");

        Storyboard.SetTarget(scaleYAnimation, RippleScale);
        Storyboard.SetTargetProperty(scaleYAnimation, "ScaleY");

        Storyboard.SetTarget(opacityAnimation, RippleEllipse);
        Storyboard.SetTargetProperty(opacityAnimation, "Opacity");

        rippleStoryboard.Children.Add(scaleXAnimation);
        rippleStoryboard.Children.Add(scaleYAnimation);
        rippleStoryboard.Children.Add(opacityAnimation);

        // Reset after animation completes
        rippleStoryboard.Completed += (s, e) =>
        {
            RippleScale.ScaleX = 1.0;
            RippleScale.ScaleY = 1.0;
            RippleEllipse.Opacity = 0.0;
        };

        rippleStoryboard.Begin();
    }
}