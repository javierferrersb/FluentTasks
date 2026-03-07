using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentTasks.UI.Models;

/// <summary>
/// Represents a navigation item in the sidebar.
/// Uses explicit properties so the WinUI XAML compiler can resolve x:Bind at compile time.
/// </summary>
public class NavItem : ObservableObject
{
    private string _id = string.Empty;
    public string Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _icon = string.Empty;
    public string Icon
    {
        get => _icon;
        set => SetProperty(ref _icon, value);
    }

    private NavItemType _type;
    public NavItemType Type
    {
        get => _type;
        set => SetProperty(ref _type, value);
    }

    private object? _data;
    public object? Data
    {
        get => _data;
        set => SetProperty(ref _data, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }
}

public enum NavItemType
{
    UserList,
    Settings,
    Sync
}