using FluentTasks.Core.Models;

namespace FluentTasks.UI.Services;

/// <summary>
/// Provides search and filtering functionality for tasks.
/// </summary>
public static class SearchService
{
    /// <summary>
    /// Filters tasks based on search query.
    /// Searches in: Title, Notes
    /// </summary>
    /// <param name="tasks">Tasks to search</param>
    /// <param name="searchQuery">Search query string</param>
    /// <returns>Filtered tasks matching the query</returns>
    public static IEnumerable<TaskItem> FilterTasks(
        IEnumerable<TaskItem> tasks,
        string searchQuery)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return tasks;

        var query = searchQuery.Trim().ToLowerInvariant();

        return tasks.Where(task =>
            task.Title.ToLowerInvariant().Contains(query) ||
            (task.Notes?.ToLowerInvariant().Contains(query) ?? false)
        );
    }
}