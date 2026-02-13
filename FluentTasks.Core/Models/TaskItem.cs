using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentTasks.Core.Models
{
    public partial class TaskItem : ObservableObject
    {
        [ObservableProperty]
        private string _id = string.Empty;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private bool _isCompleted;
    }
}
