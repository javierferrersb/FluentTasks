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

        // Update an existing task (e.g., title, notes)
        Task<bool> UpdateTaskAsync(string taskListId, TaskItem task);

        // Mark a task as completed or not completed
        Task<bool> CompleteTaskAsync(string taskListId, string taskId, bool isCompleted);

        // Delete a task
        Task<bool> DeleteTaskAsync(string taskListId, string taskId);
    }
}
