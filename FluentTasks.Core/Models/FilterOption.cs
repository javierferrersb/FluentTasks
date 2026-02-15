namespace FluentTasks.Core.Models;

/// <summary>
/// Options for filtering tasks.
/// </summary>
public enum FilterOption
{
    All,
    Incomplete,
    Completed,
    Overdue,
    Today,
    ThisWeek
}
