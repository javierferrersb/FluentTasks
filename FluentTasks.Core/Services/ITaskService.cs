using FluentTasks.Core.Models;

namespace FluentTasks.Core.Services
{
    public interface ITaskService
    {
        // Get all task lists for the user
        Task<IEnumerable<TaskList>> GetTaskListsAsync();

        // Get all tasks in a specific task list
        Task<IEnumerable<TaskItem>> GetTasksAsync(string taskListId);

        // Create a new task
        Task<TaskItem> CreateTaskAsync(string taskListId, string title);
    }
}
