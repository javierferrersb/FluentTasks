using FluentTasks.Core.Models;

namespace FluentTasks.Core.Services
{
    public interface ITaskService
    {
        // Get all task lists for the user
        Task<IEnumerable<TaskList>> GetTaskListsAsync();

        // Create a new task list
        Task<TaskList> CreateTaskListAsync(string title);

        // Update the title of an existing task list
        Task<bool> UpdateTaskListAsync(string taskListId, string newTitle);

        // Delete a task list and all its tasks
        Task<bool> DeleteTaskListAsync(string taskListId);



        // Get all tasks in a specific task list
        Task<IEnumerable<TaskItem>> GetTasksAsync(string taskListId);

        // Create a new task
        Task<TaskItem> CreateTaskAsync(string taskListId, string title, string? parentId = null, DateTimeOffset? dueDate = null);

        // Update an existing task (e.g., title, notes)
        Task<bool> UpdateTaskAsync(string taskListId, TaskItem task);

        // Mark a task as completed or not completed
        Task<bool> CompleteTaskAsync(string taskListId, string taskId, bool isCompleted);

        // Delete a task
        Task<bool> DeleteTaskAsync(string taskListId, string taskId);
    }
}
