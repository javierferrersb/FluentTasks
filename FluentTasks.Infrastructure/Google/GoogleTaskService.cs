using FluentTasks.Core.Models;
using FluentTasks.Core.Services;
using Google.Apis.Services;
using Google.Apis.Tasks.v1;
using GoogleTask = Google.Apis.Tasks.v1.Data.Task;

namespace FluentTasks.Infrastructure.Google
{
    public class GoogleTaskService : ITaskService
    {
        private readonly IGoogleAuthService _authService;

        private TasksService? _tasksService;

        public GoogleTaskService(IGoogleAuthService authService)
        {
            _authService = authService;
        }

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

        public async Task<IEnumerable<TaskList>> GetTaskListsAsync()
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

        public async Task<IEnumerable<TaskItem>> GetTasksAsync(string taskListId)
        {
            var service = await GetServiceAsync();
            var request = service.Tasks.List(taskListId);
            var response = await request.ExecuteAsync();

            // Map Google's Task to our TaskItem
            return response.Items?.Select(googleTask => new TaskItem
            {
                Id = googleTask.Id,
                Title = googleTask.Title ?? "",
                IsCompleted = googleTask.Status == "completed"
            }) ?? [];
        }

        public async Task<TaskItem> CreateTaskAsync(string taskListId, string title)
        {
            var service = await GetServiceAsync();

            var googleTask = new GoogleTask
            {
                Title = title
            };

            var request = service.Tasks.Insert(googleTask, taskListId);
            var created = await request.ExecuteAsync();

            return new TaskItem
            {
                Id = created.Id,
                Title = created.Title ?? "",
                IsCompleted = created.Status == "completed"
            };
        }
    }
}
