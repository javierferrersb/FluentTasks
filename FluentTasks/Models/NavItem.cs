namespace FluentTasks.UI.Models;

public class NavItem
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public NavItemType Type { get; set; }
    public object? Data { get; set; } // Can store TaskList or filter info
}

public enum NavItemType
{
    SmartList,
    UserList,
    Settings,
    Sync
}