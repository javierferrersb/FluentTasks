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
            TaskListTitle.Text = $"Tasks in: {selectedList.Title}";
            StatusText.Text = "Loading tasks...";
            TasksView.ItemsSource = null;

            var tasks = await _taskService.GetTasksAsync(selectedList.Id);

            TasksView.ItemsSource = tasks;
            StatusText.Text = $"Loaded {tasks.Count()} tasks";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading tasks: {ex.Message}";
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

            // Create the task in Google
            var newTask = await _taskService.CreateTaskAsync(selectedList.Id, title);

            // Get current tasks and create a NEW list with the new task
            var currentTasks = (TasksView.ItemsSource as IEnumerable<TaskItem>)?.ToList()
                ?? new List<TaskItem>();

            var updatedTasks = new List<TaskItem> { newTask }; // New task first
            updatedTasks.AddRange(currentTasks); // Then existing tasks

            // Set the new list
            TasksView.ItemsSource = updatedTasks;

            // Clear input and update status
            NewTaskInput.Text = string.Empty;
            EmptyState.Visibility = Visibility.Collapsed;
            StatusText.Text = $"Task created • {updatedTasks.Count} tasks";
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

            // Delete from Google
            var success = await _taskService.DeleteTaskAsync(selectedList.Id, task.Id);

            if (success)
            {
                // Remove from UI
                var currentTasks = (TasksView.ItemsSource as IEnumerable<TaskItem>)?.ToList();
                if (currentTasks != null)
                {
                    currentTasks.Remove(task);
                    TasksView.ItemsSource = null;
                    TasksView.ItemsSource = currentTasks;

                    // Show empty state if no tasks left
                    if (!currentTasks.Any())
                    {
                        EmptyState.Visibility = Visibility.Visible;
                    }

                    StatusText.Text = $"Task deleted • {currentTasks.Count} tasks remaining";
                }
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
}