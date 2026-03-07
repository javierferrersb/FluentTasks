using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

namespace FluentTasks.UI.Dialogs;

public sealed partial class ListEditorDialog : ContentDialog
{
    private static readonly List<string> Icons = new()
    {
        "\uE8F4", "\uE80F", "\uE8F1", "\uE719", "\uE787", "\uE8BF", "\uE7BA", "\uE734",
        "\uE82F", "\uE8A1", "\uE8D4", "\uE909", "\uE7C1", "\uE8FD", "\uE8A7", "\uE804",
        "\uE8AD", "\uE8E9", "\uE7EE", "\uE8F8", "\uE74E", "\uE8B7", "\uE82D", "\uE81E",
        "\uE71D", "\uE8F6", "\uE8B1", "\uE8B8", "\uE8A9", "\uE8E6",
    };

    public string? ListName { get; private set; }
    public string? SelectedIcon { get; private set; }

    public ListEditorDialog(string currentIcon, string currentName = "")
    {
        this.InitializeComponent();

        IconGrid.ItemsSource = Icons;
        NameTextBox.Text = currentName;

        var index = Icons.IndexOf(currentIcon);
        if (index >= 0)
        {
            IconGrid.SelectedIndex = index;
        }

        this.PrimaryButtonClick += Dialog_PrimaryButtonClick;
    }

    private void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            args.Cancel = true;
            return;
        }

        ListName = NameTextBox.Text.Trim();
        SelectedIcon = IconGrid.SelectedItem as string ?? "\uE8F4";
    }
}