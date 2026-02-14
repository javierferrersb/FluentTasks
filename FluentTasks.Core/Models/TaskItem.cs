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
        [NotifyPropertyChangedFor(nameof(IsOverdue))]
        private bool _isCompleted;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ShowAddSubtaskButton))]
        [NotifyPropertyChangedFor(nameof(ShowAddSubtaskButton))]
        private bool _isEditing;

        [ObservableProperty]
        private string _editTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsOverdue))]
        private DateTimeOffset? _dueDate;

        [ObservableProperty]
        private string? _notes;

        [ObservableProperty]
        private string? _parentId;

        [ObservableProperty]
        private string? _parentTitle;

        [ObservableProperty]
        private string _position = string.Empty;

        [ObservableProperty]
        private bool _showParentChip;

        [ObservableProperty]
        private bool _useSubtaskMargin;

        [ObservableProperty]
        private bool _canDrag;

        [ObservableProperty]
        private bool _canDrop;

        // Helper properties for display
        public bool HasDueDate => DueDate.HasValue;
        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.Today && !IsCompleted;
        public bool HasNotes => !string.IsNullOrWhiteSpace(Notes);
        public bool IsSubtask => !string.IsNullOrWhiteSpace(ParentId);
        public bool ShowAddSubtaskButton => !IsEditing && !IsSubtask;
    }
}
