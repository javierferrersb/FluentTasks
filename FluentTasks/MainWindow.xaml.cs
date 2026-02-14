using FluentTasks.Core.Models;
using FluentTasks.Core.Services;
using FluentTasks.UI.Controls;
using FluentTasks.UI.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace FluentTasks.UI;

public sealed partial class MainWindow : Window
{
    private readonly ITaskService _taskService;

    private List<TaskItem> _allTasks = new();
    private ObservableCollection<TaskList> TaskLists = new();

    private SortOption _currentSort = SortOption.None;
    private FilterOption _currentFilter = FilterOption.Incomplete;
    private TaskItem? _draggedTask;
    private string _searchQuery = string.Empty;

    public MainWindow()
    {
        this.InitializeComponent();
        _taskService = App.GetService<ITaskService>();

        // Set up custom title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
    }

    private async void LoadTaskLists_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StatusText.Text = "Syncing...";
            TaskListsView.ItemsSource = null;

            // Show empty state while loading
            NoListSelectedState.Visibility = Visibility.Visible;
            TaskContentArea.Visibility = Visibility.Collapsed;

            var taskLists = await _taskService.GetTaskListsAsync();

            TaskLists.Clear();
            foreach (var list in taskLists)
            {
                TaskLists.Add(list);
            }

            TaskListsView.ItemsSource = TaskLists;

            StatusText.Text = "Ready";
            ShowSuccess($"Synced {TaskLists.Count} lists");

            if (TaskLists.Any())
            {
                TaskListsView.SelectedIndex = 0;
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
    }

    private async void TaskListsView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TaskListsView.SelectedItem is not TaskList selectedList)
        {
            // No list selected - show empty state
            NoListSelectedState.Visibility = Visibility.Visible;
            TaskContentArea.Visibility = Visibility.Collapsed;
            return;
        }

        // List selected - show content
        NoListSelectedState.Visibility = Visibility.Collapsed;
        TaskContentArea.Visibility = Visibility.Visible;

        try
        {
            TaskListTitle.Text = selectedList.Title;
            TasksView.ItemsSource = null;
            EmptyState.Visibility = Visibility.Collapsed;

            var tasks = await _taskService.GetTasksAsync(selectedList.Id);
            _allTasks = tasks.ToList();

            foreach (var task in _allTasks.Where(t => t.IsSubtask))
            {
                var parent = _allTasks.FirstOrDefault(t => t.Id == task.ParentId);
                if (parent != null)
                {
                    task.ParentTitle = parent.Title;
                }
            }

            ApplySortAndFilter();

            // Initialize button states
            UpdateSortButtonAppearance();
            UpdateFilterButtonAppearance();

            // Set initial checkmark on filter (Incomplete is default)
            FilterIncomplete.Icon = new FontIcon { Glyph = "\uE73E" };

            ShowSuccess($"Loaded {_allTasks.Count} tasks");
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
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
            ShowInfo("Please select a list first");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewTaskInput.Text))
        {
            ShowInfo("Please enter a task title");
            return;
        }

        try
        {
            var title = NewTaskInput.Text.Trim();

            var newTask = await _taskService.CreateTaskAsync(selectedList.Id, title);

            _allTasks.Insert(0, newTask);
            ApplySortAndFilter();

            NewTaskInput.Text = string.Empty;
            ShowSuccess("Task created");
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
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
            var newCompletedState = checkbox.IsChecked == true;

            if (newCompletedState && !task.IsSubtask)
            {
                var subtasks = _allTasks.Where(t => t.ParentId == task.Id && !t.IsCompleted).ToList();

                if (subtasks.Any())
                {
                    var dialog = new ContentDialog
                    {
                        Title = "Complete subtasks?",
                        Content = $"This task has {subtasks.Count} incomplete subtask(s).\n\nCompleting the parent task will also complete all subtasks.",
                        PrimaryButtonText = "Complete All",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.Content.XamlRoot
                    };

                    var result = await dialog.ShowAsync();

                    if (result != ContentDialogResult.Primary)
                    {
                        checkbox.IsChecked = false;
                        return;
                    }
                }
            }

            var success = await _taskService.CompleteTaskAsync(
                selectedList.Id,
                task.Id,
                newCompletedState);

            if (success)
            {
                var tasks = await _taskService.GetTasksAsync(selectedList.Id);
                _allTasks = tasks.ToList();

                foreach (var t in _allTasks.Where(t => t.IsSubtask))
                {
                    var parent = _allTasks.FirstOrDefault(p => p.Id == t.ParentId);
                    if (parent != null)
                    {
                        t.ParentTitle = parent.Title;
                    }
                }

                ApplySortAndFilter();
                ShowSuccess(newCompletedState ? "Task completed" : "Task reopened");
            }
            else
            {
                checkbox.IsChecked = !newCompletedState;
                ShowWarning("Failed to update task");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
            checkbox.IsChecked = task.IsCompleted;
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
            var success = await _taskService.DeleteTaskAsync(selectedList.Id, task.Id);

            if (success)
            {
                _allTasks.Remove(task);
                ApplySortAndFilter();
                ShowSuccess("Task deleted");
            }
            else
            {
                ShowWarning("Failed to delete task");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
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
            ShowInfo("Task title cannot be empty");
            return;
        }

        try
        {
            task.Title = task.EditTitle.Trim();

            var success = await _taskService.UpdateTaskAsync(selectedList.Id, task);

            if (success)
            {
                task.IsEditing = false;
                ShowSuccess("Task updated");
            }
            else
            {
                ShowWarning("Failed to update task");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
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
                var success = await _taskService.UpdateTaskAsync(selectedList.Id, task);

                if (success)
                {
                    var tasks = await _taskService.GetTasksAsync(selectedList.Id);
                    _allTasks = tasks.ToList();

                    foreach (var t in _allTasks.Where(t => t.IsSubtask))
                    {
                        var parent = _allTasks.FirstOrDefault(p => p.Id == t.ParentId);
                        if (parent != null)
                        {
                            t.ParentTitle = parent.Title;
                        }
                    }

                    ApplySortAndFilter();
                    ShowSuccess("Task updated");
                }
                else
                {
                    ShowWarning("Failed to update task");
                }
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
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
                await _taskService.CreateTaskAsync(
                    selectedList.Id,
                    textBox.Text.Trim(),
                    parentTask.Id,
                    parentTask.DueDate);

                var tasks = await _taskService.GetTasksAsync(selectedList.Id);
                _allTasks = tasks.ToList();

                foreach (var t in _allTasks.Where(t => t.IsSubtask))
                {
                    var parent = _allTasks.FirstOrDefault(p => p.Id == t.ParentId);
                    if (parent != null)
                    {
                        t.ParentTitle = parent.Title;
                    }
                }

                ApplySortAndFilter();
                ShowSuccess("Subtask created");
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
        }
    }

    private void ApplySortAndFilter()
    {
        if (TasksView == null || TaskCountText == null)
            return;

        if (!_allTasks.Any())
        {
            TasksView.ItemsSource = null;
            TaskCountText.Text = "";
            return;
        }

        var tasks = _allTasks.AsEnumerable();

        // Apply search first
        if (!string.IsNullOrWhiteSpace(_searchQuery))
        {
            tasks = SearchService.FilterTasks(tasks, _searchQuery);
        }

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
            _ => OrganizeTasksHierarchically(tasks)
        };

        var filteredList = tasks.ToList();

        // Set display properties based on sort mode
        bool isDefaultSort = _currentSort == SortOption.None;
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

            task.CanDrag = isDefaultSort && string.IsNullOrWhiteSpace(_searchQuery);  // Can't drag when searching
            task.CanDrop = isDefaultSort && string.IsNullOrWhiteSpace(_searchQuery);
        }

        TasksView.ItemsSource = filteredList;

        // Update count
        var totalCount = _allTasks.Count;
        var displayCount = filteredList.Count;

        bool hasActiveFilters = !string.IsNullOrWhiteSpace(_searchQuery) ||
                               _currentFilter != FilterOption.Incomplete ||
                               _currentSort != SortOption.None;

        if (displayCount == totalCount && !hasActiveFilters)
        {
            TaskCountText.Text = $"{totalCount} tasks";
        }
        else
        {
            TaskCountText.Text = $"Showing {displayCount} of {totalCount} tasks";
        }

        if (EmptyState != null)
        {
            EmptyState.Visibility = filteredList.Any() ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            _searchQuery = sender.Text;
            ApplySortAndFilter();
        }
    }

    private async void CreateList_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog
        {
            Title = "Create New List",
            PrimaryButtonText = "Create",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.Content.XamlRoot
        };

        var textBox = new TextBox
        {
            PlaceholderText = "List name...",
            Text = "New List"
        };
        textBox.SelectAll();

        dialog.Content = textBox;

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            try
            {
                var newList = await _taskService.CreateTaskListAsync(textBox.Text.Trim());

                TaskLists.Add(newList);
                TaskListsView.SelectedItem = newList;

                ShowSuccess("List created");
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
        }
    }

    private async void RenameList_Click(object sender, RoutedEventArgs e)
    {
        if (TaskListsView.SelectedItem is not TaskList selectedList)
            return;

        var dialog = new ContentDialog
        {
            Title = "Rename List",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.Content.XamlRoot
        };

        var textBox = new TextBox
        {
            Text = selectedList.Title
        };
        textBox.SelectAll();

        dialog.Content = textBox;

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(textBox.Text))
        {
            try
            {
                var success = await _taskService.UpdateTaskListAsync(selectedList.Id, textBox.Text.Trim());

                if (success)
                {
                    selectedList.Title = textBox.Text.Trim();
                    TaskListTitle.Text = selectedList.Title;

                    var currentSelection = TaskListsView.SelectedItem;
                    TaskListsView.ItemsSource = null;
                    TaskListsView.ItemsSource = TaskLists;
                    TaskListsView.SelectedItem = currentSelection;

                    ShowSuccess("List renamed");
                }
                else
                {
                    ShowWarning("Failed to rename list");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
        }
    }

    private async void DeleteList_Click(object sender, RoutedEventArgs e)
    {
        if (TaskListsView.SelectedItem is not TaskList selectedList)
            return;

        var confirmDialog = new ContentDialog
        {
            Title = "Delete List?",
            Content = $"Are you sure you want to delete \"{selectedList.Title}\"?\n\nAll tasks in this list will be permanently deleted.",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.Content.XamlRoot
        };

        var result = await confirmDialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            try
            {
                var success = await _taskService.DeleteTaskListAsync(selectedList.Id);

                if (success)
                {
                    TaskLists.Remove(selectedList);

                    _allTasks.Clear();
                    TasksView.ItemsSource = null;
                    TaskListTitle.Text = "Select a list";
                    TaskCountText.Text = "";

                    ShowSuccess("List deleted");
                }
                else
                {
                    ShowWarning("Failed to delete list");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
        }
    }

    private IEnumerable<TaskItem> OrganizeTasksHierarchically(IEnumerable<TaskItem> tasks)
    {
        var tasksList = tasks.OrderBy(t => t.Position).ToList();  // Sort by position first!
        var result = new List<TaskItem>();
        var processed = new HashSet<string>();

        // First pass: add all parent tasks (non-subtasks) and their children
        foreach (var task in tasksList.Where(t => !t.IsSubtask))
        {
            if (!processed.Contains(task.Id))
            {
                result.Add(task);
                processed.Add(task.Id);

                // Add all subtasks of this parent immediately after, sorted by position
                var subtasks = tasksList
                    .Where(t => t.ParentId == task.Id)
                    .OrderBy(t => t.Position)  // Sort subtasks by position
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

        // Second pass: add any orphaned subtasks
        foreach (var task in tasksList.Where(t => t.IsSubtask && !processed.Contains(t.Id)))
        {
            result.Add(task);
            processed.Add(task.Id);
        }

        return result;
    }

    private void Task_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is Border border && border.Tag is TaskItem task)
        {
            _draggedTask = task;
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;

            // Set text for data package (fixes empty icon issue)
            args.Data.SetText(task.Title);
        }
    }

    private void Task_DragOver(object sender, DragEventArgs e)
    {
        if (_draggedTask == null)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            return;
        }

        if (sender is Border border && border.Tag is TaskItem targetTask)
        {
            // Validate drop target
            if (!IsValidDropTarget(_draggedTask, targetTask))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                return;
            }

            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.DragUIOverride.IsCaptionVisible = false;
            e.DragUIOverride.IsGlyphVisible = false;
        }
    }

    private async void Task_Drop(object sender, DragEventArgs e)
    {
        if (_draggedTask == null)
            return;

        if (sender is Border border && border.Tag is TaskItem targetTask)
        {
            if (!IsValidDropTarget(_draggedTask, targetTask))
                return;

            await MoveTaskToPosition(_draggedTask, targetTask);
        }

        _draggedTask = null;
    }

    private bool IsValidDropTarget(TaskItem draggedTask, TaskItem targetTask)
    {
        // Can't drop on itself
        if (draggedTask.Id == targetTask.Id)
            return false;

        // If dragging a parent task
        if (!draggedTask.IsSubtask)
        {
            // Can only drop on other parent tasks (not subtasks)
            if (targetTask.IsSubtask)
                return false;

            // Can't drop on own subtasks
            if (targetTask.ParentId == draggedTask.Id)
                return false;
        }
        else // Dragging a subtask
        {
            // Can only drop on siblings (same parent)
            if (draggedTask.ParentId != targetTask.ParentId)
                return false;
        }

        return true;
    }

    private async Task MoveTaskToPosition(TaskItem draggedTask, TaskItem targetTask)
    {
        if (TaskListsView.SelectedItem is not TaskList selectedList)
            return;

        try
        {
            var currentList = (TasksView.ItemsSource as IEnumerable<TaskItem>)?.ToList();
            if (currentList == null)
                return;

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

            var success = await _taskService.MoveTaskAsync(
                selectedList.Id,
                draggedTask.Id,
                previousTaskId);

            if (success)
            {
                var tasks = await _taskService.GetTasksAsync(selectedList.Id);
                _allTasks = tasks.ToList();

                foreach (var t in _allTasks.Where(t => t.IsSubtask))
                {
                    var parent = _allTasks.FirstOrDefault(p => p.Id == t.ParentId);
                    if (parent != null)
                    {
                        t.ParentTitle = parent.Title;
                    }
                }

                ApplySortAndFilter();
                ShowSuccess("Task reordered");
            }
            else
            {
                ShowWarning("Failed to reorder");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Error: {ex.Message}");
        }
    }

    private void TaskCard_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        }
    }

    private void TaskCard_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["LayerFillColorDefaultBrush"];
        }
    }

    private async void ShowTemporaryStatus(string message)
    {
        ActionText.Text = $"• {message}";

        // Fade in animation
        var fadeInStoryboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        var fadeIn = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase
            {
                EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut
            }
        };

        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeIn, ActionText);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeIn, "Opacity");
        fadeInStoryboard.Children.Add(fadeIn);
        fadeInStoryboard.Begin();

        // Wait 3 seconds, then fade out
        await System.Threading.Tasks.Task.Delay(3000);

        var fadeOutStoryboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        var fadeOut = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase
            {
                EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseIn
            }
        };

        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeOut, ActionText);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeOut, "Opacity");
        fadeOutStoryboard.Children.Add(fadeOut);
        fadeOutStoryboard.Begin();
    }

    private void SortMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item &&
            Enum.TryParse<SortOption>(item.Tag?.ToString(), out var sortOption))
        {
            _currentSort = sortOption;

            // Update all menu items - remove checkmarks
            SortDefault.Icon = null;
            SortDueDateAsc.Icon = null;
            SortDueDateDesc.Icon = null;
            SortAlpha.Icon = null;
            SortAlphaRev.Icon = null;
            SortCompleted.Icon = null;

            // Add checkmark to selected item
            item.Icon = new FontIcon { Glyph = "\uE73E" };

            // Update button appearance
            UpdateSortButtonAppearance();

            ApplySortAndFilter();
        }
    }

    private void FilterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item &&
            Enum.TryParse<FilterOption>(item.Tag?.ToString(), out var filterOption))
        {
            _currentFilter = filterOption;

            // Update all menu items - remove checkmarks
            FilterAll.Icon = null;
            FilterIncomplete.Icon = null;
            FilterCompleted.Icon = null;
            FilterOverdue.Icon = null;
            FilterToday.Icon = null;
            FilterWeek.Icon = null;

            // Add checkmark to selected item
            item.Icon = new FontIcon { Glyph = "\uE73E" };

            // Update button appearance
            UpdateFilterButtonAppearance();

            ApplySortAndFilter();
        }
    }

    private string GetSortDisplayText(SortOption sort)
    {
        return sort switch
        {
            SortOption.None => "Default",
            SortOption.DueDateAscending => "Due date (earliest)",
            SortOption.DueDateDescending => "Due date (latest)",
            SortOption.Alphabetical => "A to Z",
            SortOption.AlphabeticalReverse => "Z to A",
            SortOption.CompletedLast => "Completed last",
            _ => "Sort"
        };
    }

    private string GetFilterDisplayText(FilterOption filter)
    {
        return filter switch
        {
            FilterOption.All => "All tasks",
            FilterOption.Incomplete => "Incomplete",
            FilterOption.Completed => "Completed",
            FilterOption.Overdue => "Overdue",
            FilterOption.Today => "Due today",
            FilterOption.ThisWeek => "Due this week",
            _ => "Filter"
        };
    }

    private void UpdateSortButtonAppearance()
    {
        bool isActive = _currentSort != SortOption.None;

        if (isActive)
        {
            SortButtonText.Text = $"Sort: {GetSortDisplayText(_currentSort)}";
            SortButtonText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"];
        }
        else
        {
            SortButtonText.Text = "Sort";
            SortButtonText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        }
    }

    private void UpdateFilterButtonAppearance()
    {
        bool isActive = _currentFilter != FilterOption.Incomplete;

        if (isActive)
        {
            FilterButtonText.Text = $"Filter: {GetFilterDisplayText(_currentFilter)}";
            FilterButtonText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"];
        }
        else
        {
            FilterButtonText.Text = "Filter";
            FilterButtonText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
        }
    }

    /// <summary>
    /// Shows a success state with ripple effect and temporary message
    /// </summary>
    private void ShowSuccess(string message)
    {
        StatusOrb.TriggerRipple();
        ShowTemporaryStatus(message);
    }

    /// <summary>
    /// Shows an error state with offline orb and message
    /// </summary>
    private void ShowError(string message)
    {
        StatusOrb.SetStatus(OrbStatus.Offline);
        StatusText.Text = "Offline";
        ShowTemporaryStatus(message);
    }

    /// <summary>
    /// Shows a warning state with yellow orb and message
    /// </summary>
    private void ShowWarning(string message)
    {
        StatusOrb.SetStatus(OrbStatus.Warning);
        ShowTemporaryStatus(message);
    }

    /// <summary>
    /// Shows an info message without changing orb state
    /// </summary>
    private void ShowInfo(string message)
    {
        ShowTemporaryStatus(message);
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
