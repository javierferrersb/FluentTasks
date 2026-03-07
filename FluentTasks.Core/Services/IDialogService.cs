namespace FluentTasks.Core.Services;

/// <summary>
/// Represents the result of a list editor dialog.
/// </summary>
/// <param name="Name">The list name entered by the user, or null if cancelled.</param>
/// <param name="Icon">The icon glyph selected by the user.</param>
public record ListEditorResult(string? Name, string? Icon);

/// <summary>
/// Abstracts dialog interactions so ViewModels remain UI-framework-agnostic.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Shows a confirmation dialog and returns true if the user confirmed.
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string content, string confirmText = "OK", string cancelText = "Cancel");

    /// <summary>
    /// Shows the list editor dialog for creating or editing a list.
    /// </summary>
    Task<ListEditorResult> ShowListEditorAsync(string currentIcon, string currentName = "");

    /// <summary>
    /// Shows a text input dialog (e.g., for adding a subtask) and returns the entered text, or null if cancelled.
    /// </summary>
    Task<string?> ShowTextInputAsync(string title, string placeholder);

    /// <summary>
    /// Shows the task details dialog and returns true if the user saved changes.
    /// </summary>
    Task<bool> ShowTaskDetailsAsync(object task);
}
