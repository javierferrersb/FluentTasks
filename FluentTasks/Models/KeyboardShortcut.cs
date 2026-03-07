using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentTasks.UI.Models;

/// <summary>
/// Represents a single keyboard shortcut with its display key combination and description.
/// </summary>
internal sealed record KeyboardShortcut(string Category, string Keys, string Description);

/// <summary>
/// Central registry of all keyboard shortcuts, grouped by category.
/// Used to populate the shortcuts overlay and to document available actions.
/// </summary>
internal static class KeyboardShortcutRegistry
{
    private static readonly ResourceLoader s_resources = new();

    internal static string CategoryNavigation => s_resources.GetString("ShortcutsCategoryNavigation");
    internal static string CategoryTasks => s_resources.GetString("ShortcutsCategoryTasks");
    internal static string CategoryView => s_resources.GetString("ShortcutsCategoryView");

    private static ImmutableArray<KeyboardShortcut> BuildShortcuts()
    {
        var nav = CategoryNavigation;
        var tasks = CategoryTasks;
        var view = CategoryView;

        return
        [
            // Navigation
            new(nav, "\u2191 / \u2193", s_resources.GetString("ShortcutDescNavigateTasks")),
            new(nav, "Ctrl+F", s_resources.GetString("ShortcutDescFocusSearchBox")),
            new(nav, "Escape", s_resources.GetString("ShortcutDescClearSelection")),

            // Tasks
            new(tasks, "Ctrl+N", s_resources.GetString("ShortcutDescCreateNewTask")),
            new(tasks, "Ctrl+Shift+N", s_resources.GetString("ShortcutDescCreateNewList")),
            new(tasks, "Enter / F2", s_resources.GetString("ShortcutDescEditSelectedTask")),
            new(tasks, "Space", s_resources.GetString("ShortcutDescToggleCompletion")),
            new(tasks, "Ctrl+D", s_resources.GetString("ShortcutDescToggleCompletionAlt")),
            new(tasks, "Delete", s_resources.GetString("ShortcutDescDeleteSelectedTask")),
            new(tasks, "Ctrl+E", s_resources.GetString("ShortcutDescOpenTaskDetails")),
            new(tasks, "Ctrl+Shift+S", s_resources.GetString("ShortcutDescAddSubtask")),

            // View & App
            new(view, "Ctrl+R", s_resources.GetString("ShortcutDescSyncRefresh")),
            new(view, "Ctrl+,", s_resources.GetString("ShortcutDescOpenSettings")),
            new(view, "Ctrl+Shift+?", s_resources.GetString("ShortcutDescShowShortcuts")),
            new(view, "?", s_resources.GetString("ShortcutDescShowShortcuts")),
            new(view, "Ctrl+W", s_resources.GetString("ShortcutDescCloseApp")),
        ];
    }

    /// <summary>
    /// Returns all registered shortcuts grouped by category in display order.
    /// </summary>
    internal static IReadOnlyList<KeyboardShortcut> GetAll() => BuildShortcuts();

    /// <summary>
    /// Returns shortcuts filtered by category name.
    /// </summary>
    internal static ImmutableArray<KeyboardShortcut> GetByCategory(string category)
        => [.. BuildShortcuts().Where(s => s.Category == category)];
}
