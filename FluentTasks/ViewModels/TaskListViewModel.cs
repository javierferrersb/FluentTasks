using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentTasks.Core.Models;
using FluentTasks.Core.Services;
using FluentTasks.UI.Services;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentTasks.UI.ViewModels;

/// <summary>
/// ViewModel for displaying and managing a list of tasks.
/// </summary>
public sealed partial class TaskListViewModel : ObservableObject
{
    private readonly ITaskService _taskService;
    private readonly IDialogService _dialogService;
    private readonly ResourceLoader _resourceLoader;

    private List<TaskItem> _allTasks = [];
    private TaskList? _selectedList;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private ObservableCollection<TaskItem> _tasks = [];

    [ObservableProperty]
    private SortOption _currentSort = SortOption.None;

    [ObservableProperty]
    private FilterOption _currentFilter = FilterOption.Incomplete;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isEmpty;

    [ObservableProperty]
    private bool _showAddTaskInput;

    [ObservableProperty]
    private string _newTaskTitle = string.Empty;

    [ObservableProperty]
    private string _sortButtonText = string.Empty;

    [ObservableProperty]
    private bool _isSortActive;

    [ObservableProperty]
    private string _filterButtonText = string.Empty;

    [ObservableProperty]
    private bool _isFilterActive;

    [ObservableProperty]
    private string _emptyStateIcon = "✓";

    [ObservableProperty]
    private string _emptyStateTitle = string.Empty;

    [ObservableProperty]
    private string _emptyStateSubtitle = string.Empty;

    /// <summary>
    /// Whether this is a smart list (aggregated from all lists) vs a single user list.
    /// </summary>
    public bool IsSmartList { get; set; }

    /// <summary>
    /// Raised to communicate status messages to the parent shell.
    /// </summary>
    public event EventHandler<StatusMessageEventArgs>? StatusMessage;

    /// <summary>
    /// Raised when a data-modifying operation completes successfully and sync is needed.
    /// </summary>
    public event EventHandler? SyncRequested;

    public TaskListViewModel(ITaskService taskService, IDialogService dialogService)
    {
        _taskService = taskService;
        _dialogService = dialogService;
        _resourceLoader = new ResourceLoader();

        _sortButtonText = GetResource("TaskListSortBase", "Sort");
        _filterButtonText = GetResource("TaskListFilterBase", "Filter");
        _emptyStateTitle = GetResource("TaskListEmptyStateDefaultTitle", "No tasks");
        _emptyStateSubtitle = GetResource("TaskListEmptyStateDefaultSubtitle", "Add a task to get started");
    }

    /// <summary>
    /// Sets the display title for the task list.
    /// </summary>
    public void SetTitle(string title) => Title = title;

    /// <summary>
    /// Sets the backing TaskList (null for smart lists).
    /// </summary>
    public void SetSelectedList(TaskList? list)
    {
        _selectedList = list;
        ShowAddTaskInput = list is not null;
    }

    /// <summary>
    /// Shows skeleton loading state.
    /// </summary>
    public void BeginLoading()
    {
        IsLoading = true;
        IsEmpty = false;
        Tasks.Clear();
    }

    /// <summary>
    /// Hides skeleton loading state.
    /// </summary>
    public void EndLoading() => IsLoading = false;

    /// <summary>
    /// Loads tasks into the view model and applies sort/filter.
    /// </summary>
    public void LoadTasks(List<TaskItem> tasks, SortOption sort, FilterOption filter)
    {
        _allTasks = tasks;
        PopulateParentTitles();

        CurrentSort = sort;
        CurrentFilter = filter;

        IsLoading = false;
        ApplySortAndFilter();
        UpdateButtonAppearances();
    }

    /// <summary>
    /// Applies the current sort, filter, and search to produce the displayed task list.
    /// </summary>
    public void ApplySortAndFilter()
    {
        if (_allTasks.Count == 0)
        {
            Tasks = [];
            IsEmpty = true;
            UpdateEmptyStateText();
            return;
        }

        var tasks = _allTasks.AsEnumerable();

        // Search
        if (!string.IsNullOrWhiteSpace(SearchQuery))
        {
            tasks = SearchService.FilterTasks(tasks, SearchQuery);
        }

        // Filter
        tasks = CurrentFilter switch
        {
            FilterOption.Incomplete => tasks.Where(t => !t.IsCompleted),
            FilterOption.Completed => tasks.Where(t => t.IsCompleted),
            FilterOption.Overdue => tasks.Where(t => t.IsOverdue),
            FilterOption.Today => tasks.Where(t => t.DueDate.HasValue &&
                t.DueDate.Value.Date == DateTimeOffset.Now.Date),
            FilterOption.ThisWeek => tasks.Where(t => t.DueDate.HasValue &&
                t.DueDate.Value.Date >= DateTimeOffset.Now.Date &&
                t.DueDate.Value.Date <= DateTimeOffset.Now.Date.AddDays(7)),
            _ => tasks
        };

        // Sort
        tasks = CurrentSort switch
        {
            SortOption.DueDateAscending => tasks.OrderBy(t => t.DueDate ?? DateTimeOffset.MaxValue),
            SortOption.DueDateDescending => tasks.OrderByDescending(t => t.DueDate ?? DateTimeOffset.MinValue),
            SortOption.Alphabetical => tasks.OrderBy(t => t.Title),
            SortOption.AlphabeticalReverse => tasks.OrderByDescending(t => t.Title),
            SortOption.CompletedLast => tasks.OrderBy(t => t.IsCompleted).ThenBy(t => t.Title),
            _ => OrganizeTasksHierarchically(tasks)
        };

        var filteredList = tasks.ToList();

        // Set display properties based on sort mode
        bool isDefaultSort = CurrentSort == SortOption.None;
        bool canDragDrop = isDefaultSort && string.IsNullOrWhiteSpace(SearchQuery);

        foreach (var task in filteredList)
        {
            if (task.IsSubtask)
            {
                task.ShowParentChip = !isDefaultSort;
                task.UseSubtaskMargin = isDefaultSort;
            }
            else
            {
                task.ShowParentChip = false;
                task.UseSubtaskMargin = false;
            }

            task.CanDrag = canDragDrop;
            task.CanDrop = canDragDrop;
        }

        Tasks = new ObservableCollection<TaskItem>(filteredList);
        IsEmpty = filteredList.Count == 0;
        UpdateEmptyStateText();
    }

    /// <summary>
    /// Called when the search text changes.
    /// </summary>
    public void UpdateSearch(string query)
    {
        SearchQuery = query;
        ApplySortAndFilter();
    }

    private void UpdateEmptyStateText()
    {
        if (!IsEmpty)
        {
            return;
        }

        bool hasSearchQuery = !string.IsNullOrWhiteSpace(SearchQuery);
        bool hasActiveFilter = CurrentFilter != FilterOption.All && CurrentFilter != FilterOption.Incomplete;

        if (hasSearchQuery)
        {
            EmptyStateIcon = "🔍";
            EmptyStateTitle = GetResource("TaskListEmptyStateNoResultsTitle", "No results found");
            EmptyStateSubtitle = GetResource("TaskListEmptyStateNoResultsSubtitle", "Try a different search term");
        }
        else if (hasActiveFilter)
        {
            EmptyStateIcon = "🔍";
            EmptyStateTitle = GetResource("TaskListEmptyStateNoMatchingTitle", "No matching tasks");
            EmptyStateSubtitle = GetResource("TaskListEmptyStateNoMatchingSubtitle", "Try changing or clearing the filter");
        }
        else
        {
            EmptyStateIcon = "✓";
            EmptyStateTitle = GetResource("TaskListEmptyStateDefaultTitle", "No tasks");
            EmptyStateSubtitle = GetResource("TaskListEmptyStateDefaultSubtitle", "Add a task to get started");
        }
    }

    /// <summary>
    /// Changes the sort option and refreshes.
    /// </summary>
    public void SetSort(SortOption sort)
    {
        CurrentSort = sort;
        UpdateButtonAppearances();
        ApplySortAndFilter();
    }

    /// <summary>
    /// Changes the filter option and refreshes.
    /// </summary>
    public void SetFilter(FilterOption filter)
    {
        CurrentFilter = filter;
        UpdateButtonAppearances();
        ApplySortAndFilter();
    }

    /// <summary>
    /// Creates a new task in the current list.
    /// </summary>
    [RelayCommand]
    private async Task AddTaskAsync()
    {
        if (_selectedList is null)
        {
            RaiseStatus(StatusKind.Info, GetResource("TaskListStatusSelectListFirst", "Please select a list first"));
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTaskTitle))
        {
            RaiseStatus(StatusKind.Info, GetResource("TaskListStatusEnterTaskTitle", "Please enter a task title"));
            return;
        }

        try
        {
            var title = NewTaskTitle.Trim();
            var newTask = await _taskService.CreateTaskAsync(_selectedList.Id, title);

            _allTasks.Insert(0, newTask);
            ApplySortAndFilter();

            NewTaskTitle = string.Empty;
            RaiseStatus(StatusKind.Success, GetResource("TaskListStatusTaskCreated", "Task created"));
            SyncRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            RaiseStatus(StatusKind.Error, string.Format(GetResource("TaskListStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    /// <summary>
    /// Toggles the completion state of a task.
    /// Returns false if the operation was cancelled by the user.
    /// </summary>
    public async Task<bool> CompleteTaskAsync(TaskItem task, bool newCompletedState)
    {
        if (_selectedList is null)
            return false;

        try
        {
            // If completing a parent task with incomplete subtasks, confirm
            if (newCompletedState && !task.IsSubtask)
            {
                var subtasks = _allTasks.Where(t => t.ParentId == task.Id && !t.IsCompleted).ToList();
                if (subtasks.Count > 0)
                {
                    var confirmed = await _dialogService.ShowConfirmationAsync(
                        GetResource("TaskListConfirmCompleteSubtasksTitle", "Complete subtasks?"),
                        string.Format(
                            GetResource("TaskListConfirmCompleteSubtasksMessage", "This task has {0} incomplete subtask(s).\n\nCompleting the parent task will also complete all subtasks."),
                            subtasks.Count),
                        GetResource("TaskListConfirmCompleteSubtasksPrimary", "Complete All"),
                        GetResource("TaskListConfirmCompleteSubtasksClose", "Cancel"));

                    if (!confirmed) return false;
                }
            }

            var success = await _taskService.CompleteTaskAsync(_selectedList.Id, task.Id, newCompletedState);

            if (success)
            {
                await ReloadTasksAsync();
                RaiseStatus(StatusKind.Success, newCompletedState
                    ? GetResource("TaskListStatusTaskCompleted", "Task completed")
                    : GetResource("TaskListStatusTaskReopened", "Task reopened"));
                SyncRequested?.Invoke(this, EventArgs.Empty);
                return true;
            }

            RaiseStatus(StatusKind.Warning, GetResource("TaskListStatusFailedUpdateTask", "Failed to update task"));
            return false;
        }
        catch (Exception ex)
        {
            RaiseStatus(StatusKind.Error, string.Format(GetResource("TaskListStatusErrorFormat", "Error: {0}"), ex.Message));
            return false;
        }
    }

    /// <summary>
    /// Deletes a task from the current list.
    /// </summary>
    public async Task DeleteTaskAsync(TaskItem task)
    {
        if (_selectedList is null)
            return;

        try
        {
            var success = await _taskService.DeleteTaskAsync(_selectedList.Id, task.Id);

            if (success)
            {
                _allTasks.Remove(task);
                ApplySortAndFilter();
                RaiseStatus(StatusKind.Success, GetResource("TaskListStatusTaskDeleted", "Task deleted"));
                SyncRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                RaiseStatus(StatusKind.Warning, GetResource("TaskListStatusFailedDeleteTask", "Failed to delete task"));
            }
        }
        catch (Exception ex)
        {
            RaiseStatus(StatusKind.Error, string.Format(GetResource("TaskListStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    /// <summary>
    /// Enters edit mode for a task.
    /// </summary>
    public void BeginEditTask(TaskItem task)
    {
        // Exit edit mode for all other tasks
        foreach (var t in _allTasks.Where(t => t.IsEditing && t.Id != task.Id))
        {
            t.IsEditing = false;
            t.EditTitle = string.Empty;
        }

        task.EditTitle = task.Title;
        task.IsEditing = true;
    }

    /// <summary>
    /// Saves the in-progress title edit for a task.
    /// </summary>
    public async Task SaveEditAsync(TaskItem task)
    {
        if (_selectedList is null)
            return;

        if (string.IsNullOrWhiteSpace(task.EditTitle))
        {
            RaiseStatus(StatusKind.Info, GetResource("TaskListStatusTaskTitleEmpty", "Task title cannot be empty"));
            return;
        }

        try
        {
            task.Title = task.EditTitle.Trim();
            var success = await _taskService.UpdateTaskAsync(_selectedList.Id, task);

            if (success)
            {
                task.IsEditing = false;
                RaiseStatus(StatusKind.Success, GetResource("TaskListStatusTaskUpdated", "Task updated"));
                SyncRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                RaiseStatus(StatusKind.Warning, GetResource("TaskListStatusFailedUpdateTask", "Failed to update task"));
            }
        }
        catch (Exception ex)
        {
            RaiseStatus(StatusKind.Error, string.Format(GetResource("TaskListStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    /// <summary>
    /// Cancels the in-progress title edit.
    /// </summary>
    public void CancelEdit(TaskItem task)
    {
        task.IsEditing = false;
        task.EditTitle = string.Empty;
    }

    /// <summary>
    /// Shows the task details dialog and saves if changed.
    /// </summary>
    public async Task ShowDetailsAsync(TaskItem task)
    {
        if (_selectedList is null)
            return;

        try
        {
            var saved = await _dialogService.ShowTaskDetailsAsync(task);
            if (saved)
            {
                var success = await _taskService.UpdateTaskAsync(_selectedList.Id, task);
                if (success)
                {
                    await ReloadTasksAsync();
                    RaiseStatus(StatusKind.Success, GetResource("TaskListStatusTaskUpdated", "Task updated"));
                    SyncRequested?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    RaiseStatus(StatusKind.Warning, GetResource("TaskListStatusFailedUpdateTask", "Failed to update task"));
                }
            }
        }
        catch (Exception ex)
        {
            RaiseStatus(StatusKind.Error, string.Format(GetResource("TaskListStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    /// <summary>
    /// Adds a subtask to a parent task via dialog.
    /// </summary>
    public async Task AddSubtaskAsync(TaskItem parentTask)
    {
        if (_selectedList is null)
            return;

        var subtaskTitle = await _dialogService.ShowTextInputAsync(
            string.Format(GetResource("TaskListAddSubtaskDialogTitleFormat", "Add subtask to: {0}"), parentTask.Title),
            GetResource("TaskListAddSubtaskDialogPlaceholder", "Subtask title..."));

        if (subtaskTitle is null)
            return;

        try
        {
            await _taskService.CreateTaskAsync(
                _selectedList.Id,
                subtaskTitle,
                parentTask.Id,
                parentTask.DueDate);

            await ReloadTasksAsync();
            RaiseStatus(StatusKind.Success, GetResource("TaskListStatusSubtaskCreated", "Subtask created"));
            SyncRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            RaiseStatus(StatusKind.Error, string.Format(GetResource("TaskListStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    /// <summary>
    /// Moves a dragged task to the position of a target task.
    /// </summary>
    public async Task MoveTaskAsync(TaskItem draggedTask, TaskItem targetTask)
    {
        if (_selectedList is null)
            return;

        if (!IsValidDropTarget(draggedTask, targetTask))
            return;

        try
        {
            var currentList = Tasks.ToList();
            var draggedIndex = currentList.IndexOf(draggedTask);
            var targetIndex = currentList.IndexOf(targetTask);

            if (draggedIndex == -1 || targetIndex == -1)
                return;

            string? previousTaskId = null;

            if (draggedIndex < targetIndex)
            {
                previousTaskId = targetTask.Id;
            }
            else
            {
                var previousIndex = targetIndex - 1;
                while (previousIndex >= 0)
                {
                    var candidate = currentList[previousIndex];

                    if (draggedTask.IsSubtask)
                    {
                        if (candidate.ParentId == draggedTask.ParentId)
                        {
                            previousTaskId = candidate.Id;
                            break;
                        }
                    }
                    else
                    {
                        if (!candidate.IsSubtask)
                        {
                            previousTaskId = candidate.Id;
                            break;
                        }
                    }

                    previousIndex--;
                }
            }

            var success = await _taskService.MoveTaskAsync(_selectedList.Id, draggedTask.Id, previousTaskId);

            if (success)
            {
                await ReloadTasksAsync();
                RaiseStatus(StatusKind.Success, GetResource("TaskListStatusTaskReordered", "Task reordered"));
                SyncRequested?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                RaiseStatus(StatusKind.Warning, GetResource("TaskListStatusFailedReorderTask", "Failed to reorder"));
            }
        }
        catch (Exception ex)
        {
            RaiseStatus(StatusKind.Error, string.Format(GetResource("TaskListStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    /// <summary>
    /// Validates whether a task can be dropped onto a target task.
    /// </summary>
    public static bool IsValidDropTarget(TaskItem draggedTask, TaskItem targetTask)
    {
        if (draggedTask.Id == targetTask.Id)
            return false;

        if (!draggedTask.IsSubtask)
        {
            if (targetTask.IsSubtask)
                return false;
            if (targetTask.ParentId == draggedTask.Id)
                return false;
        }
        else
        {
            if (draggedTask.ParentId != targetTask.ParentId)
                return false;
        }

        return true;
    }

    private async Task ReloadTasksAsync()
    {
        if (_selectedList is null) return;

        var tasks = await _taskService.GetTasksAsync(_selectedList.Id);
        _allTasks = tasks.ToList();
        PopulateParentTitles();
        ApplySortAndFilter();
    }

    private void PopulateParentTitles()
    {
        foreach (var task in _allTasks.Where(t => t.IsSubtask))
        {
            var parent = _allTasks.FirstOrDefault(t => t.Id == task.ParentId);
            if (parent is not null)
            {
                task.ParentTitle = parent.Title;
            }
        }
    }

    private static IEnumerable<TaskItem> OrganizeTasksHierarchically(IEnumerable<TaskItem> tasks)
    {
        var tasksList = tasks.OrderBy(t => t.Position).ToList();
        var result = new List<TaskItem>();
        var processed = new HashSet<string>();

        foreach (var task in tasksList.Where(t => !t.IsSubtask))
        {
            if (!processed.Contains(task.Id))
            {
                result.Add(task);
                processed.Add(task.Id);

                var subtasks = tasksList
                    .Where(t => t.ParentId == task.Id)
                    .OrderBy(t => t.Position)
                    .ToList();

                foreach (var subtask in subtasks)
                {
                    if (!processed.Contains(subtask.Id))
                    {
                        result.Add(subtask);
                        processed.Add(subtask.Id);
                    }
                }
            }
        }

        // Orphaned subtasks
        foreach (var task in tasksList.Where(t => t.IsSubtask && !processed.Contains(t.Id)))
        {
            result.Add(task);
            processed.Add(task.Id);
        }

        return result;
    }

    private void UpdateButtonAppearances()
    {
        IsSortActive = CurrentSort != SortOption.None;
        SortButtonText = IsSortActive
            ? string.Format(GetResource("TaskListSortLabelFormat", "{0}: {1}"), GetResource("TaskListSortBase", "Sort"), GetSortDisplayText(CurrentSort))
            : GetResource("TaskListSortBase", "Sort");

        IsFilterActive = CurrentFilter != FilterOption.Incomplete;
        FilterButtonText = IsFilterActive
            ? string.Format(GetResource("TaskListFilterLabelFormat", "{0}: {1}"), GetResource("TaskListFilterBase", "Filter"), GetFilterDisplayText(CurrentFilter))
            : GetResource("TaskListFilterBase", "Filter");
    }

    private string GetSortDisplayText(SortOption sort) => sort switch
    {
        SortOption.None => GetResource("TaskListSortOptionDefault", "Default"),
        SortOption.DueDateAscending => GetResource("TaskListSortOptionDueDateAsc", "Due date (earliest)"),
        SortOption.DueDateDescending => GetResource("TaskListSortOptionDueDateDesc", "Due date (latest)"),
        SortOption.Alphabetical => GetResource("TaskListSortOptionAlphabetical", "A to Z"),
        SortOption.AlphabeticalReverse => GetResource("TaskListSortOptionAlphabeticalReverse", "Z to A"),
        SortOption.CompletedLast => GetResource("TaskListSortOptionCompletedLast", "Completed last"),
        _ => GetResource("TaskListSortBase", "Sort")
    };

    private string GetFilterDisplayText(FilterOption filter) => filter switch
    {
        FilterOption.All => GetResource("TaskListFilterOptionAll", "All tasks"),
        FilterOption.Incomplete => GetResource("TaskListFilterOptionIncomplete", "Incomplete"),
        FilterOption.Completed => GetResource("TaskListFilterOptionCompleted", "Completed"),
        FilterOption.Overdue => GetResource("TaskListFilterOptionOverdue", "Overdue"),
        FilterOption.Today => GetResource("TaskListFilterOptionToday", "Due today"),
        FilterOption.ThisWeek => GetResource("TaskListFilterOptionThisWeek", "Due this week"),
        _ => GetResource("TaskListFilterBase", "Filter")
    };

    private string GetResource(string key, string fallback)
    {
        var value = _resourceLoader.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private void RaiseStatus(StatusKind kind, string message)
    {
        StatusMessage?.Invoke(this, new StatusMessageEventArgs(kind, message));
    }
}
