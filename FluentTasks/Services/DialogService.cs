using System;
using System.Threading.Tasks;
using FluentTasks.Core.Models;
using FluentTasks.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentTasks.UI.Services;

/// <summary>
/// WinUI 3 implementation of <see cref="IDialogService"/>.
/// Requires <see cref="SetXamlRoot"/> to be called before any dialog can be shown.
/// </summary>
internal sealed class DialogService : IDialogService
{
    private XamlRoot? _xamlRoot;
    private ElementTheme _currentTheme = ElementTheme.Default;
    private readonly ResourceLoader _resourceLoader = new();

    /// <summary>
    /// True while a ContentDialog is being shown.
    /// </summary>
    public bool IsDialogOpen { get; private set; }

    /// <summary>
    /// Sets the <see cref="XamlRoot"/> used for presenting dialogs.
    /// Must be called once after the main window content is loaded.
    /// </summary>
    public void SetXamlRoot(XamlRoot xamlRoot)
    {
        _xamlRoot = xamlRoot;
    }

    /// <summary>
    /// Updates the current theme for dialogs.
    /// </summary>
    public void SetTheme(ElementTheme theme)
    {
        _currentTheme = theme;
    }

    /// <inheritdoc />
    public async Task<bool> ShowConfirmationAsync(string title, string content, string confirmText = "", string cancelText = "")
    {
        var primaryText = string.IsNullOrWhiteSpace(confirmText)
            ? GetResource("DialogDefaultConfirm", "OK")
            : confirmText;
        var closeText = string.IsNullOrWhiteSpace(cancelText)
            ? GetResource("DialogDefaultCancel", "Cancel")
            : cancelText;

        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = GetXamlRoot(),
            RequestedTheme = _currentTheme,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"]
        };

        IsDialogOpen = true;
        try
        {
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
        finally
        {
            IsDialogOpen = false;
        }
    }

    /// <inheritdoc />
    public async Task<ListEditorResult> ShowListEditorAsync(string currentIcon, string currentName = "")
    {
        var dialog = new Dialogs.ListEditorDialog(currentIcon, currentName)
        {
            XamlRoot = GetXamlRoot(),
            RequestedTheme = _currentTheme
        };

        IsDialogOpen = true;
        ContentDialogResult result;
        try
        {
            result = await dialog.ShowAsync();
        }
        finally
        {
            IsDialogOpen = false;
        }

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
            PrimaryButtonText = GetResource("DialogTextInputPrimary", "Add"),
            CloseButtonText = GetResource("DialogDefaultCancel", "Cancel"),
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = GetXamlRoot(),
            RequestedTheme = _currentTheme,
            Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"]
        };

        IsDialogOpen = true;
        ContentDialogResult result;
        try
        {
            result = await dialog.ShowAsync();
        }
        finally
        {
            IsDialogOpen = false;
        }

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
            XamlRoot = GetXamlRoot(),
            RequestedTheme = _currentTheme
        };

        IsDialogOpen = true;
        try
        {
            var result = await dialog.ShowAsync();
            return result == ContentDialogResult.Primary;
        }
        finally
        {
            IsDialogOpen = false;
        }
    }

    private XamlRoot GetXamlRoot()
    {
        return _xamlRoot ?? throw new InvalidOperationException(
            GetResource("DialogServiceXamlRootNotSetError", "XamlRoot has not been set. Call SetXamlRoot first."));
    }

    private string GetResource(string key, string fallback)
    {
        var value = _resourceLoader.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
