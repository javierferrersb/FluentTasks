using FluentTasks.Core.Models;
using FluentTasks.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentTasks.UI;

public sealed partial class MainWindow : Window
{
    private readonly ITaskService _taskService;

    private List<TaskItem> _allTasks = new();
    private SortOption _currentSort = SortOption.None;
    private FilterOption _currentFilter = FilterOption.All;

    public MainWindow()
    {
        this.InitializeComponent();
        _taskService = App.GetService<ITaskService>();
    }

    private async void LoadTaskLists_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Loading task lists...";
            TaskListsView.ItemsSource = null;
            TasksView.ItemsSource = null;

            var taskLists = await _taskService.GetTaskListsAsync();

            TaskListsView.ItemsSource = taskLists;
            StatusText.Text = $"Loaded {taskLists.Count()} task lists. Click one to see tasks.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async void TaskListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TaskListsView.SelectedItem is not TaskList selectedList)
            return;

        try
        {
            TaskListTitle.Text = selectedList.Title;
            StatusText.Text = "Loading tasks...";
            TasksView.ItemsSource = null;
            EmptyState.Visibility = Visibility.Collapsed;

            var tasks = await _taskService.GetTasksAsync(selectedList.Id);
            _allTasks = tasks.ToList();

            // Populate ParentTitle for subtasks
            foreach (var task in _allTasks.Where(t => t.IsSubtask))
            {
                var parent = _allTasks.FirstOrDefault(t => t.Id == task.ParentId);
                if (parent != null)
                {
                    task.ParentTitle = parent.Title;
                }
            }

            // Apply current sort and filter
            ApplySortAndFilter();

            StatusText.Text = $"{_allTasks.Count} tasks loaded";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async void AddTask_Click(object sender, RoutedEventArgs e)
    {
        await CreateNewTaskAsync();
    }

    private async void AddTask_Enter(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await CreateNewTaskAsync();
        args.Handled = true;
    }

    private async Task CreateNewTaskAsync()
    {
        if (TaskListsView.SelectedItem is not TaskList selectedList)
        {
            StatusText.Text = "Please select a list first";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTaskInput.Text))
        {
            StatusText.Text = "Please enter a task title";
            return;
        }

        try
        {
            var title = NewTaskInput.Text.Trim();
            StatusText.Text = "Creating task...";

            var newTask = await _taskService.CreateTaskAsync(selectedList.Id, title);

            // Add to all tasks
            _allTasks.Insert(0, newTask);

            // Reapply sort/filter
            ApplySortAndFilter();

            NewTaskInput.Text = string.Empty;
            StatusText.Text = $"Task created • {_allTasks.Count} tasks";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error creating task: {ex.Message}";
        }
    }

    private async void TaskCheckbox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkbox || checkbox.Tag is not TaskItem task)
            return;

        if (TaskListsView.SelectedItem is not TaskList selectedList)
            return;

        try
        {
            var newCompletedState = task.IsCompleted; // Already updated by binding

            StatusText.Text = newCompletedState ? "Completing task..." : "Reopening task...";

            // Update in Google Tasks
            var success = await _taskService.CompleteTaskAsync(
                selectedList.Id,
                task.Id,
                newCompletedState);

            if (success)
            {
                StatusText.Text = newCompletedState
                    ? "Task completed ✓"
                    : "Task reopened";
            }
            else
            {
                // Revert if failed
                task.IsCompleted = !newCompletedState;
                StatusText.Text = "Failed to update task";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
            // Revert on error
            task.IsCompleted = !task.IsCompleted;
        }
    }

    private async void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TaskItem task)
            return;

        if (TaskListsView.SelectedItem is not TaskList selectedList)
            return;

        try
        {
            StatusText.Text = "Deleting task...";

            var success = await _taskService.DeleteTaskAsync(selectedList.Id, task.Id);

            if (success)
            {
                // Remove from all tasks
                _allTasks.Remove(task);

                // Reapply sort/filter
                ApplySortAndFilter();

                StatusText.Text = $"Task deleted • {_allTasks.Count} tasks remaining";
            }
            else
            {
                StatusText.Text = "Failed to delete task";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void TaskTitle_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock || textBlock.Tag is not TaskItem task)
            return;

        // Enter edit mode
        task.EditTitle = task.Title;
        task.IsEditing = true;
    }

    private async void SaveEdit_Click(object sender, RoutedEventArgs e)
    {
        await SaveTaskEditAsync(sender);
    }

    private async void SaveEdit_Enter(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await SaveTaskEditAsync(args.Element);
        args.Handled = true;
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        CancelTaskEdit(sender);
    }

    private void CancelEdit_Escape(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        CancelTaskEdit(args.Element);
        args.Handled = true;
    }

    private async Task SaveTaskEditAsync(object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not TaskItem task)
            return;

        if (TaskListsView.SelectedItem is not TaskList selectedList)
            return;

        if (string.IsNullOrWhiteSpace(task.EditTitle))
        {
            StatusText.Text = "Task title cannot be empty";
            return;
        }

        try
        {
            StatusText.Text = "Updating task...";

            // Update the title
            task.Title = task.EditTitle.Trim();

            // Update in Google
            var success = await _taskService.UpdateTaskAsync(selectedList.Id, task);

            if (success)
            {
                task.IsEditing = false;
                StatusText.Text = "Task updated ✓";
            }
            else
            {
                StatusText.Text = "Failed to update task";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void CancelTaskEdit(object sender)
    {
        if (sender is not FrameworkElement element || element.Tag is not TaskItem task)
            return;

        // Exit edit mode without saving
        task.IsEditing = false;
        task.EditTitle = string.Empty;
    }

    private async void TaskDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TaskItem task)
            return;

        if (TaskListsView.SelectedItem is not TaskList selectedList)
            return;

        try
        {
            var dialog = new Dialogs.TaskDetailsDialog(task);
            dialog.XamlRoot = this.Content.XamlRoot;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                StatusText.Text = "Updating task...";

                var success = await _taskService.UpdateTaskAsync(selectedList.Id, task);

                if (success)
                {
                    var tasks = await _taskService.GetTasksAsync(selectedList.Id);
                    _allTasks = tasks.ToList();

                    // Populate ParentTitle for subtasks
                    foreach (var t in _allTasks.Where(t => t.IsSubtask))
                    {
                        var parent = _allTasks.FirstOrDefault(p => p.Id == t.ParentId);
                        if (parent != null)
                        {
                            t.ParentTitle = parent.Title;
                        }
                    }

                    // Apply current sort/filter
                    ApplySortAndFilter();

                    StatusText.Text = "Task updated ✓";
                }
                else
                {
                    StatusText.Text = "Failed to update task";
                }
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private async void AddSubtask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TaskItem parentTask)
            return;

        if (TaskListsView.SelectedItem is not TaskList selectedList)
            return;

        var dialog = new ContentDialog
        {
            Title = $"Add subtask to: {parentTask.Title}",
            PrimaryButtonText = "Add",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.Content.XamlRoot
        };

        var textBox = new TextBox
        {
            PlaceholderText = "Subtask title..."
        };

        dialog.Content = textBox;

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            try
            {
                StatusText.Text = "Creating subtask...";

                var newSubtask = await _taskService.CreateTaskAsync(
                    selectedList.Id,
                    textBox.Text.Trim(),
                    parentTask.Id,
                    parentTask.DueDate);

                IEnumerable<TaskItem>? tasks = await _taskService.GetTasksAsync(selectedList.Id);
                _allTasks = tasks.ToList();

                // Populate ParentTitle for subtasks
                foreach (var t in _allTasks.Where(t => t.IsSubtask))
                {
                    var parent = _allTasks.FirstOrDefault(p => p.Id == t.ParentId);
                    if (parent != null)
                    {
                        t.ParentTitle = parent.Title;
                    }
                }

                // Apply current sort/filter
                ApplySortAndFilter();

                StatusText.Text = $"Subtask created • {_allTasks.Count} tasks";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }
    }

    private void Sort_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SortComboBox.SelectedItem is ComboBoxItem item &&
            Enum.TryParse<SortOption>(item.Tag?.ToString(), out var sortOption))
        {
            _currentSort = sortOption;
            ApplySortAndFilter();
        }
    }

    private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterComboBox.SelectedItem is ComboBoxItem item &&
            Enum.TryParse<FilterOption>(item.Tag?.ToString(), out var filterOption))
        {
            _currentFilter = filterOption;
            ApplySortAndFilter();
        }
    }

    private void ApplySortAndFilter()
    {
        // Guard: Don't run if UI isn't loaded yet
        if (TasksView == null || TaskCountText == null)
            return;

        if (!_allTasks.Any())
        {
            TasksView.ItemsSource = null;
            TaskCountText.Text = "";
            return;
        }

        var tasks = _allTasks.AsEnumerable();

        // Apply filter
        tasks = _currentFilter switch
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

        // Apply sort
        tasks = _currentSort switch
        {
            SortOption.DueDateAscending => tasks.OrderBy(t => t.DueDate ?? DateTimeOffset.MaxValue),
            SortOption.DueDateDescending => tasks.OrderByDescending(t => t.DueDate ?? DateTimeOffset.MinValue),
            SortOption.Alphabetical => tasks.OrderBy(t => t.Title),
            SortOption.AlphabeticalReverse => tasks.OrderByDescending(t => t.Title),
            SortOption.CompletedLast => tasks.OrderBy(t => t.IsCompleted).ThenBy(t => t.Title),
            _ => tasks
        };

        var filteredList = tasks.ToList();
        TasksView.ItemsSource = filteredList;

        // Update count
        var totalCount = _allTasks.Count;
        var displayCount = filteredList.Count;

        if (displayCount == totalCount)
        {
            TaskCountText.Text = $"{totalCount} tasks";
        }
        else
        {
            TaskCountText.Text = $"Showing {displayCount} of {totalCount} tasks";
        }

        // Show/hide empty state
        if (EmptyState != null)
        {
            EmptyState.Visibility = filteredList.Any() ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    public enum SortOption
    {
        None,
        DueDateAscending,
        DueDateDescending,
        Alphabetical,
        AlphabeticalReverse,
        CompletedLast
    }

    public enum FilterOption
    {
        All,
        Incomplete,
        Completed,
        Overdue,
        Today,
        ThisWeek
    }
}
