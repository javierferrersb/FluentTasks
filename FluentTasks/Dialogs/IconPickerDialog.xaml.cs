using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace FluentTasks.UI.Dialogs;

public sealed partial class IconPickerDialog : ContentDialog
{
    private static readonly List<string> Icons = new()
    {
        "\uE8F4", // List
        "\uE80F", // Home
        "\uE8F1", // Work/Briefcase
        "\uE719", // Shopping cart
        "\uE787", // Calendar
        "\uE8BF", // Today
        "\uE7BA", // Warning
        "\uE734", // Favorite/Star
        "\uE82F", // Mail
        "\uE8A1", // Phone
        "\uE8D4", // People
        "\uE909", // Heart
        "\uE7C1", // Lightbulb
        "\uE8FD", // Globe
        "\uE8A7", // Plane
        "\uE804", // Food
        "\uE8AD", // Music
        "\uE8E9", // Library/Books
        "\uE7EE", // Flag
        "\uE8F8", // Document
        "\uE74E", // Video
        "\uE8B7", // Camera
        "\uE82D", // Clock
        "\uE81E", // Location
        "\uE71D", // Attach
        "\uE8F6", // Folder
        "\uE8B1", // Box
        "\uE8B8", // Tag
        "\uE8A9", // Key
        "\uE8E6", // Target
    };

    public string? SelectedIcon { get; private set; }

    public IconPickerDialog(string currentIcon)
    {
        this.InitializeComponent();

        IconGrid.ItemsSource = Icons;

        // Select current icon
        var index = Icons.IndexOf(currentIcon);
        if (index >= 0)
        {
            IconGrid.SelectedIndex = index;
        }
    }

    private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (IconGrid.SelectedItem is string icon)
        {
            SelectedIcon = icon;
        }
    }
}