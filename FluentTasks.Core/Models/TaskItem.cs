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

        // Used for editing
        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editTitle = string.Empty;

        [ObservableProperty]
        private DateTimeOffset? _dueDate;

        // Helper property for display
        public bool HasDueDate => DueDate.HasValue;
        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today && !IsCompleted;
    }
}
