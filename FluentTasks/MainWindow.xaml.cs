using FluentTasks.Core.Models;
using FluentTasks.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

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
}