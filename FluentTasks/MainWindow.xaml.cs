using FluentTasks.Core.Services;
using Microsoft.UI.Xaml;
using System;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FluentTasks.UI
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly ITaskService _taskService;

        public MainWindow()
        {
            InitializeComponent();

            _taskService = App.GetService<ITaskService>();
        }

        private async void LoadTaskListsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusText.Text = "Loading... (browser may open for login)";
                TaskListsView.ItemsSource = null;

                // This will trigger OAuth if not authenticated
                var taskLists = await _taskService.GetTaskListsAsync();

                TaskListsView.ItemsSource = taskLists;
                StatusText.Text = $"Loaded {taskLists.Count()} task lists!";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
            }
        }
    }
}
