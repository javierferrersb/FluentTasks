using System;
using System.Threading.Tasks;
using FluentTasks.Core.Models;
using FluentTasks.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentTasks.UI.Services;

/// <summary>
/// WinUI 3 implementation of <see cref="IDialogService"/>.
/// Requires <see cref="SetXamlRoot"/> to be called before any dialog can be shown.
/// </summary>
internal sealed class DialogService : IDialogService
{
    private XamlRoot? _xamlRoot;

    /// <summary>
    /// Sets the <see cref="XamlRoot"/> used for presenting dialogs.
    /// Must be called once after the main window content is loaded.
    /// </summary>
    public void SetXamlRoot(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    /// <inheritdoc />
    public async Task<bool> ShowConfirmationAsync(string title, string content, string confirmText = "OK", string cancelText = "Cancel")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = confirmText,
            CloseButtonText = cancelText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    /// <inheritdoc />
    public async Task<ListEditorResult> ShowListEditorAsync(string currentIcon, string currentName = "")
    {
        var dialog = new Dialogs.ListEditorDialog(currentIcon, currentName)
        {
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && dialog.ListName is not null && dialog.SelectedIcon is not null)
        {
            return new ListEditorResult(dialog.ListName, dialog.SelectedIcon);
        }

        return new ListEditorResult(null, null);
    }

    /// <inheritdoc />
    public async Task<string?> ShowTextInputAsync(string title, string placeholder)
    {
        var textBox = new TextBox { PlaceholderText = placeholder };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = textBox,
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            return textBox.Text.Trim();
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> ShowTaskDetailsAsync(object task)
    {
        if (task is not TaskItem taskItem)
            return false;

        var dialog = new Dialogs.TaskDetailsDialog(taskItem)
        {
            XamlRoot = GetXamlRoot()
        };

        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    private XamlRoot GetXamlRoot()
    {
        return _xamlRoot ?? throw new InvalidOperationException("XamlRoot has not been set. Call SetXamlRoot first.");
    }
}
