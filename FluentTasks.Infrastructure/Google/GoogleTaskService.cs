using FluentTasks.Core.Exceptions;
using FluentTasks.Core.Models;
using FluentTasks.Core.Services;
using Google;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.Tasks.v1;
using GoogleTask = Google.Apis.Tasks.v1.Data.Task;
using GoogleTaskList = Google.Apis.Tasks.v1.Data.TaskList;

namespace FluentTasks.Infrastructure.Google
{
    /// <summary>
    /// Implementation of ITaskService that communicates with Google Tasks API.
    /// Handles all CRUD operations for task lists and tasks.
    /// </summary>
    public class GoogleTaskService : ITaskService
    {
        private readonly IGoogleAuthService _authService;

        private TasksService? _tasksService;

        public GoogleTaskService(IGoogleAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Resets the cached service so the next call creates a fresh one with new credentials.
        /// </summary>
        internal void InvalidateService()
        {
            _tasksService = null;
        }

        private static bool IsAuthenticationError(Exception ex)
        {
            return ex is TokenResponseException
                || (ex is GoogleApiException gae && gae.HttpStatusCode == System.Net.HttpStatusCode.Unauthorized);
        }

        /// <summary>
        /// Wraps an auth-related exception as <see cref="AuthenticationExpiredException"/>.
        /// Also resets the cached service so the next call can use refreshed credentials.
        /// </summary>
        private AuthenticationExpiredException WrapAuthError(Exception ex)
        {
            _tasksService = null;
            return new AuthenticationExpiredException(
                "Your session has expired. Please sign in again.", ex);
        }

        /// <summary>
        /// Gets or initializes the Google Tasks API service.
        /// Lazy initialization pattern - only creates service once and reuses it.
        /// Requires valid OAuth credentials from the auth service.
        /// </summary>
        /// <returns>Configured TasksService instance</returns>
        private async Task<TasksService> GetServiceAsync()
        {
            if (_tasksService != null)
            {
                return _tasksService;
            }

            var credential = await _authService.GetCredentialAsync();

            _tasksService = new TasksService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "FluentTasks"
            });

            return _tasksService;
        }

        /// <summary>
        /// Retrieves all task lists for the authenticated user from Google Tasks.
        /// Returns an empty collection if no lists exist or if the request fails.
        /// </summary>
        /// <returns>Collection of task lists with ID and Title</returns>
        public async Task<IEnumerable<TaskList>> GetTaskListsAsync()
        {
            try
            {
                var service = await GetServiceAsync();
                var request = service.Tasklists.List();
                var response = await request.ExecuteAsync();

                return response.Items?.Select(tl => new TaskList
                {
                    Id = tl.Id,
                    Title = tl.Title
                }) ?? [];
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
        }

        /// <summary>
        /// Retrieves all tasks from a specific task list.
        /// Includes completed tasks and maps Google's task structure to our domain model.
        /// Parses due dates from RFC 3339 format.
        /// </summary>
        /// <param name="taskListId">The ID of the task list to retrieve tasks from</param>
        /// <returns>Collection of tasks with ID, Title, Completion status, and Due date</returns>
        public async Task<IEnumerable<TaskItem>> GetTasksAsync(string taskListId)
        {
            try
            {
                var service = await GetServiceAsync();
                var request = service.Tasks.List(taskListId);
                var response = await request.ExecuteAsync();

                return response.Items?.Select(googleTask => new TaskItem
                {
                    Id = googleTask.Id,
                    Title = googleTask.Title ?? "",
                    IsCompleted = googleTask.Status == "completed",
                    DueDate = string.IsNullOrEmpty(googleTask.Due)
                        ? null
                        : DateTimeOffset.Parse(googleTask.Due),
                    Notes = googleTask.Notes,
                    ParentId = googleTask.Parent,
                    Position = googleTask.Position ?? "0"
                }) ?? [];
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
        }

        /// <summary>
        /// Creates a new task in the specified task list.
        /// Initially creates task with only a title - other properties can be set via UpdateTaskAsync.
        /// The task is added to Google Tasks and immediately returned with its generated ID.
        /// </summary>
        /// <param name="taskListId">The task list to add the task to</param>
        /// <param name="title">The task title</param>
        /// <param name="dueDate">Optional due date for the task</param>
        /// <param name="parentId">Optional parent task ID for creating a subtask</param>
        /// <returns>Newly created task with Google-generated ID</returns>
        public async Task<TaskItem> CreateTaskAsync(string taskListId, string title, string? parentId = null, DateTimeOffset? dueDate = null)
        {
            try
            {
            var service = await GetServiceAsync();

            var googleTask = new GoogleTask
            {
                Title = title,
                Parent = parentId
            };

            // Set due date if provided
            if (dueDate.HasValue)
            {
                googleTask.Due = dueDate.Value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
            }

            var request = service.Tasks.Insert(googleTask, taskListId);

            // If it's a subtask, specify the parent
            if (!string.IsNullOrEmpty(parentId))
            {
                request.Parent = parentId;
            }

            var created = await request.ExecuteAsync();

            return new TaskItem
            {
                Id = created.Id,
                Title = created.Title ?? "",
                IsCompleted = created.Status == "completed",
                DueDate = string.IsNullOrEmpty(created.Due)
                    ? null
                    : DateTimeOffset.Parse(created.Due),
                Notes = created.Notes,
                ParentId = created.Parent,
                Position = created.Position ?? "0"
            };
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
        }

        /// <summary>
        /// Updates an existing task in Google Tasks.
        /// 
        /// IMPORTANT: Uses fetch-modify-update pattern to preserve all fields.
        /// Google Tasks API replaces the entire task on update, so we must:
        /// 1. GET the current task (preserves Notes, Parent, Links, etc.)
        /// 2. Modify only the fields we want to change
        /// 3. Send the complete task back
        /// 
        /// This prevents accidental deletion of fields like Notes (description).
        /// </summary>
        /// <param name="taskListId">The task list containing the task</param>
        /// <param name="task">The task with updated values</param>
        /// <returns>True if update succeeded, false otherwise</returns>
        public async Task<bool> UpdateTaskAsync(string taskListId, TaskItem task)
        {
            try
            {
                var service = await GetServiceAsync();

                // STEP 1: Get the current task from Google (preserves all fields)
                var getRequest = service.Tasks.Get(taskListId, task.Id);
                var googleTask = await getRequest.ExecuteAsync();

                // STEP 2: Update only the fields we care about
                googleTask.Title = task.Title;
                googleTask.Status = task.IsCompleted ? "completed" : "needsAction";
                googleTask.Notes = task.Notes;

                // Update due date if we have it
                if (task.DueDate.HasValue)
                {
                    googleTask.Due = task.DueDate.Value.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
                }
                else
                {
                    googleTask.Due = null;
                }

                // Notes, Parent, Position, etc. are preserved from the GET

                // STEP 3: Send the complete task back
                var updateRequest = service.Tasks.Update(googleTask, taskListId, task.Id);
                await updateRequest.ExecuteAsync();
                return true;
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Marks a task as completed or reopens it.
        /// Sets the completion timestamp when completing a task.
        /// Also uses fetch-modify-update pattern to preserve other fields.
        /// </summary>
        /// <param name="taskListId">The task list containing the task</param>
        /// <param name="taskId">The ID of the task to update</param>
        /// <param name="isCompleted">True to mark as completed, false to reopen</param>
        /// <returns>True if update succeeded, false otherwise</returns>
        public async Task<bool> CompleteTaskAsync(string taskListId, string taskId, bool isCompleted)
        {
            try
            {
                var service = await GetServiceAsync();

                // Get the task first
                var getRequest = service.Tasks.Get(taskListId, taskId);
                var task = await getRequest.ExecuteAsync();

                // Update status
                task.Status = isCompleted ? "completed" : "needsAction";

                if (isCompleted)
                {
                    task.Completed = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'");
                }
                else
                {
                    task.Completed = null;
                }

                var updateRequest = service.Tasks.Update(task, taskListId, taskId);
                await updateRequest.ExecuteAsync();

                return true;
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Permanently deletes a task from Google Tasks.
        /// This action cannot be undone - the task is completely removed.
        /// </summary>
        /// <param name="taskListId">The task list containing the task</param>
        /// <param name="taskId">The ID of the task to delete</param>
        /// <returns>True if deletion succeeded, false otherwise</returns>
        public async Task<bool> DeleteTaskAsync(string taskListId, string taskId)
        {
            try
            {
                var service = await GetServiceAsync();
                var request = service.Tasks.Delete(taskListId, taskId);
                await request.ExecuteAsync();
                return true;
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Creates a new task list.
        /// </summary>
        /// <param name="title">The name of the new task list</param>
        /// <returns>The created task list with Google-generated ID</returns>
        public async Task<TaskList> CreateTaskListAsync(string title)
        {
            try
            {
                var service = await GetServiceAsync();

                var googleTaskList = new GoogleTaskList
                {
                    Title = title
                };

                var request = service.Tasklists.Insert(googleTaskList);
                var created = await request.ExecuteAsync();

                return new TaskList
                {
                    Id = created.Id,
                    Title = created.Title
                };
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create task list", ex);
            }
        }

        /// <summary>
        /// Updates the title of a task list.
        /// Uses fetch-modify-update pattern to preserve other properties.
        /// </summary>
        /// <param name="taskListId">ID of the task list to update</param>
        /// <param name="newTitle">New title for the task list</param>
        /// <returns>True if update succeeded, false otherwise</returns>
        public async Task<bool> UpdateTaskListAsync(string taskListId, string newTitle)
        {
            try
            {
                var service = await GetServiceAsync();

                // Get current task list
                var getRequest = service.Tasklists.Get(taskListId);
                var taskList = await getRequest.ExecuteAsync();

                // Update title
                taskList.Title = newTitle;

                // Send back
                var updateRequest = service.Tasklists.Update(taskList, taskListId);
                await updateRequest.ExecuteAsync();

                return true;
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes a task list and all its tasks.
        /// WARNING: This permanently deletes all tasks in the list.
        /// </summary>
        /// <param name="taskListId">ID of the task list to delete</param>
        /// <returns>True if deletion succeeded, false otherwise</returns>
        public async Task<bool> DeleteTaskListAsync(string taskListId)
        {
            try
            {
                var service = await GetServiceAsync();
                var request = service.Tasklists.Delete(taskListId);
                await request.ExecuteAsync();
                return true;
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Moves a task to a new position in the list.
        /// Used for drag-and-drop reordering.
        /// </summary>
        /// <param name="taskListId">The task list containing the task</param>
        /// <param name="taskId">The task to move</param>
        /// <param name="previousTaskId">The task that should come before this one (null = move to top)</param>
        /// <returns>True if move succeeded, false otherwise</returns>
        public async Task<bool> MoveTaskAsync(string taskListId, string taskId, string? previousTaskId)
        {
            try
            {
                var service = await GetServiceAsync();

                var request = service.Tasks.Move(taskListId, taskId);

                // Set the task that should come before this one
                if (!string.IsNullOrEmpty(previousTaskId))
                {
                    request.Previous = previousTaskId;
                }

                await request.ExecuteAsync();
                return true;
            }
            catch (Exception ex) when (IsAuthenticationError(ex))
            {
                throw WrapAuthError(ex);
            }
            catch
            {
                return false;
            }
        }
    }
}
