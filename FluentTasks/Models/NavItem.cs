using System.ComponentModel;

namespace FluentTasks.UI.Models;

public class NavItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public NavItemType Type { get; set; }
    public object? Data { get; set; } // Can store TaskList or filter info
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}

public enum NavItemType
{
    SmartList,
    UserList,
    Settings,
    Sync
}