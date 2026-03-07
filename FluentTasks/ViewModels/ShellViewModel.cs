using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentTasks.Core.Exceptions;
using FluentTasks.Core.Models;
using FluentTasks.Core.Services;
using FluentTasks.Infrastructure.Google;
using FluentTasks.UI.Models;
using FluentTasks.UI.Services;
using Microsoft.UI.Xaml;
using Microsoft.Windows.ApplicationModel.Resources;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FluentTasks.UI.ViewModels;

/// <summary>
/// ViewModel for the main shell window.
/// Manages navigation, task lists, and sync operations.
/// </summary>
public sealed partial class ShellViewModel : ObservableObject
{
    private readonly ITaskService _taskService;
    private readonly IDialogService _dialogService;
    private readonly IGoogleAuthService _authService;
    private readonly IconStorageService _iconStorageService;
    private readonly SettingsService _settingsService;
    private readonly ResourceLoader _resourceLoader = new();
    private readonly ObservableCollection<TaskList> _taskListsBackingStore = [];

    // Auto-sync infrastructure
    private DispatcherTimer? _autoSyncTimer;
    private DispatcherTimer? _debounceTimer;
    private EventHandler<object>? _autoSyncTickHandler;
    private EventHandler<object>? _debounceTickHandler;
    private bool _isSyncing;
    private int _autoSyncIntervalMinutes = 5;
    private const int MinSyncIntervalMinutes = 2;
    private const int MaxSyncIntervalMinutes = 60;

    [ObservableProperty]
    private DateTimeOffset? _lastSyncTime;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AutoSyncIntervalMinutes))]
    private bool _isAutoSyncEnabled = true;

    /// <summary>
    /// Gets or sets the auto-sync interval in minutes, clamped between MIN_SYNC_INTERVAL_MINUTES and MAX_SYNC_INTERVAL_MINUTES.
    /// </summary>
    public int AutoSyncIntervalMinutes
    {
        get => _autoSyncIntervalMinutes;
        set
        {
            _autoSyncIntervalMinutes = Math.Clamp(value, MinSyncIntervalMinutes, MaxSyncIntervalMinutes);
            OnPropertyChanged();
        }
    }

    [ObservableProperty]
    private bool _isInitialLoading = true;

    [ObservableProperty]
    private ObservableCollection<NavItem> _userLists = [];

    [ObservableProperty]
    private NavItem? _selectedNavItem;

    [ObservableProperty]
    private string _statusText = string.Empty;

    [ObservableProperty]
    private bool _isNoListSelected = true;

    [ObservableProperty]
    private bool _isTaskContentVisible;

    /// <summary>
    /// Raised when a status ripple animation should play.
    /// </summary>
    public event Action? RippleRequested;

    /// <summary>
    /// Raised when the status orb state should change.
    /// </summary>
    public event Action<OrbStatusKind>? OrbStatusChanged;

    /// <summary>
    /// Raised when a temporary action message should animate in and out.
    /// </summary>
    public event Action<string>? TemporaryStatusRequested;

    /// <summary>
    /// The child view model for the active task list.
    /// </summary>
    [ObservableProperty]
    private TaskListViewModel? _taskListVM;

    public ShellViewModel(
        ITaskService taskService,
        IDialogService dialogService,
        IGoogleAuthService authService,
        IconStorageService iconStorageService,
        SettingsService settingsService)
    {
        _taskService = taskService;
        _dialogService = dialogService;
        _authService = authService;
        _iconStorageService = iconStorageService;
        _settingsService = settingsService;
        _statusText = GetResource("ShellStatusReady", "Ready");
    }

    [RelayCommand]
    private async Task SyncAsync()
    {
        if (_isSyncing)
            return;

        try
        {
            _isSyncing = true;
            OrbStatusChanged?.Invoke(OrbStatusKind.Syncing);
            StatusText = GetResource("ShellStatusSyncing", "Syncing...");

            var selectedId = SelectedNavItem?.Id;

            var taskLists = await _taskService.GetTaskListsAsync();

            _taskListsBackingStore.Clear();
            UserLists.Clear();

            foreach (var list in taskLists)
            {
                _taskListsBackingStore.Add(list);

                var icon = _iconStorageService.GetIcon(list.Id);
                var navItem = new NavItem
                {
                    Id = list.Id,
                    Title = list.Title,
                    Icon = icon,
                    Type = NavItemType.UserList,
                    Data = list
                };

                if (navItem.Id == selectedId)
                {
                    navItem.IsSelected = true;
                    SelectedNavItem = navItem;
                }

                UserLists.Add(navItem);
            }

            await RefreshCurrentViewAsync();

            LastSyncTime = DateTimeOffset.Now;
            OrbStatusChanged?.Invoke(OrbStatusKind.Connected);
            StatusText = GetResource("ShellStatusSynced", "Synced");
            ShowSuccess(string.Format(GetResource("ShellStatusSyncedListsFormat", "Synced {0} lists"), taskLists.Count()));
        }
        catch (HttpRequestException ex)
        {
            OrbStatusChanged?.Invoke(OrbStatusKind.Offline);
            StatusText = GetResource("ShellStatusOffline", "Offline");
            ShowError(string.Format(GetResource("ShellStatusNetworkErrorFormat", "Network error: {0}"), ex.Message));
        }
        catch (AuthenticationExpiredException)
        {
            await HandleAuthenticationExpiredAsync();
        }
        catch (Exception ex)
        {
            OrbStatusChanged?.Invoke(OrbStatusKind.Warning);
            StatusText = GetResource("ShellStatusSyncFailed", "Sync failed");
            ShowError(string.Format(GetResource("ShellStatusSyncErrorFormat", "Sync error: {0}"), ex.Message));
        }
        finally
        {
            _isSyncing = false;
        }
    }

    [RelayCommand]
    private async Task CreateListAsync()
    {
        var result = await _dialogService.ShowListEditorAsync("\uE8F4", GetResource("ShellNewListDefaultName", "New List"));
        if (result.Name is null)
            return;

        try
        {
            var newList = await _taskService.CreateTaskListAsync(result.Name);
            _iconStorageService.SetIcon(newList.Id, result.Icon ?? "\uE8F4");

            _taskListsBackingStore.Add(newList);
            UserLists.Add(new NavItem
            {
                Id = newList.Id,
                Title = newList.Title,
                Icon = result.Icon ?? "\uE8F4",
                Type = NavItemType.UserList,
                Data = newList
            });

            ShowSuccess(GetResource("ShellStatusListCreated", "List created"));
            ScheduleSyncAfterChange();
        }
        catch (Exception ex)
        {
            ShowError(string.Format(GetResource("ShellStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    /// <summary>
    /// Selects a navigation item and triggers the appropriate content load.
    /// </summary>
    public async Task SelectNavItemAsync(NavItem navItem)
    {
        // Deselect all
        foreach (var item in UserLists) item.IsSelected = false;

        navItem.IsSelected = true;
        SelectedNavItem = navItem;

        IsNoListSelected = false;
        IsTaskContentVisible = true;

        if (navItem.Type == NavItemType.UserList)
        {
            await HandleUserListSelectionAsync(navItem);
        }
    }

    private async Task HandleUserListSelectionAsync(NavItem navItem)
    {
        if (navItem.Data is not TaskList selectedList)
            return;

        EnsureTaskListVM(isSmartList: false);
        TaskListVM!.SetTitle(selectedList.Title);
        TaskListVM.SetSelectedList(selectedList);

        try
        {
            TaskListVM.BeginLoading();

            var tasks = await _taskService.GetTasksAsync(selectedList.Id);
            TaskListVM.LoadTasks(tasks.ToList(), _settingsService.DefaultSort, _settingsService.DefaultFilter);

            StatusText = GetResource("ShellStatusReady", "Ready");
        }
        catch (Exception ex)
        {
            TaskListVM.EndLoading();
            ShowError(string.Format(GetResource("ShellStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    private void EnsureTaskListVM(bool isSmartList)
    {
        if (TaskListVM is null)
        {
            TaskListVM = new TaskListViewModel(_taskService, _dialogService);
            TaskListVM.StatusMessage += OnChildStatus;
            TaskListVM.SyncRequested += (s, e) => ScheduleSyncAfterChange();
        }

        TaskListVM.IsSmartList = isSmartList;
    }

    private void OnChildStatus(object? sender, StatusMessageEventArgs e)
    {
        switch (e.Kind)
        {
            case StatusKind.Success:
                ShowSuccess(e.Message);
                break;
            case StatusKind.Warning:
                ShowWarning(e.Message);
                break;
            case StatusKind.Error:
                ShowError(e.Message);
                break;
            case StatusKind.Info:
                ShowInfo(e.Message);
                break;
        }
    }

    /// <summary>
    /// Renames an existing task list via dialog.
    /// </summary>
    public async Task RenameListAsync(NavItem navItem)
    {
        if (navItem.Data is not TaskList selectedList)
            return;

        var result = await _dialogService.ShowListEditorAsync(navItem.Icon, selectedList.Title);
        if (result.Name is null)
            return;

        try
        {
            var success = await _taskService.UpdateTaskListAsync(selectedList.Id, result.Name);

            if (success)
            {
                selectedList.Title = result.Name;
                navItem.Title = result.Name;
                navItem.Icon = result.Icon ?? navItem.Icon;

                _iconStorageService.SetIcon(selectedList.Id, result.Icon ?? navItem.Icon);

                if (TaskListVM is not null && SelectedNavItem == navItem)
                {
                    TaskListVM.SetTitle(result.Name);
                }

                // Force collection refresh
                RefreshUserLists();
                ShowSuccess(GetResource("ShellStatusListUpdated", "List updated"));
                ScheduleSyncAfterChange();
            }
            else
            {
                ShowWarning(GetResource("ShellStatusFailedUpdateList", "Failed to update list"));
            }
        }
        catch (Exception ex)
        {
            ShowError(string.Format(GetResource("ShellStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    /// <summary>
    /// Deletes a task list after confirmation.
    /// </summary>
    public async Task DeleteListAsync(NavItem navItem)
    {
        if (navItem.Data is not TaskList selectedList)
            return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            GetResource("ShellDeleteListDialogTitle", "Delete List?"),
            string.Format(
                GetResource("ShellDeleteListDialogMessageFormat", "Are you sure you want to delete \"{0}\"?\n\nAll tasks in this list will be permanently deleted."),
                selectedList.Title),
            GetResource("ShellDeleteListDialogPrimary", "Delete"),
            GetResource("ShellDeleteListDialogClose", "Cancel"));

        if (!confirmed)
            return;

        try
        {
            var success = await _taskService.DeleteTaskListAsync(selectedList.Id);

            if (success)
            {
                _taskListsBackingStore.Remove(selectedList);
                UserLists.Remove(navItem);

                if (SelectedNavItem == navItem)
                {
                    SelectedNavItem = null;
                    TaskListVM = null;
                    IsNoListSelected = true;
                    IsTaskContentVisible = false;
                }

                ShowSuccess(GetResource("ShellStatusListDeleted", "List deleted"));
                ScheduleSyncAfterChange();
            }
            else
            {
                ShowWarning(GetResource("ShellStatusFailedDeleteList", "Failed to delete list"));
            }
        }
        catch (Exception ex)
        {
            ShowError(string.Format(GetResource("ShellStatusErrorFormat", "Error: {0}"), ex.Message));
        }
    }

    private void RefreshUserLists()
    {
        // Trigger change notification by swapping the collection
        var items = UserLists.ToList();
        UserLists.Clear();
        foreach (var item in items) UserLists.Add(item);
    }

    // --- Auto-Sync Infrastructure ---

    /// <summary>
    /// Initializes and starts the automatic background synchronization timer.
    /// Should be called once during application startup.
    /// </summary>
    public void InitializeAutoSync()
    {
        if (!_isAutoSyncEnabled)
            return;

        _autoSyncTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(AutoSyncIntervalMinutes)
        };
        _autoSyncTickHandler = async (s, e) => await PerformAutoSyncAsync();
        _autoSyncTimer.Tick += _autoSyncTickHandler;
        _autoSyncTimer.Start();

        // Perform initial sync on startup
        _ = PerformInitialSyncAsync();
    }

    /// <summary>
    /// Stops the auto-sync timer and performs cleanup.
    /// Should be called when the application is closing.
    /// </summary>
    public void StopAutoSync()
    {
        if (_autoSyncTimer is not null)
        {
            _autoSyncTimer.Stop();
            if (_autoSyncTickHandler is not null)
            {
                _autoSyncTimer.Tick -= _autoSyncTickHandler;
                _autoSyncTickHandler = null;
            }
            _autoSyncTimer = null;
        }

        if (_debounceTimer is not null)
        {
            _debounceTimer.Stop();
            if (_debounceTickHandler is not null)
            {
                _debounceTimer.Tick -= _debounceTickHandler;
                _debounceTickHandler = null;
            }
            _debounceTimer = null;
        }
    }

    /// <summary>
    /// Performs automatic background synchronization of task lists and tasks.
    /// Updates UI with sync status and handles errors gracefully.
    /// </summary>
    private async Task PerformAutoSyncAsync()
    {
        // Prevent overlapping sync operations
        if (_isSyncing)
            return;

        try
        {
            _isSyncing = true;
            OrbStatusChanged?.Invoke(OrbStatusKind.Syncing);
            StatusText = GetResource("ShellStatusSyncing", "Syncing...");

            var selectedId = SelectedNavItem?.Id;

            // Sync task lists
            var taskLists = await _taskService.GetTaskListsAsync();

            _taskListsBackingStore.Clear();
            UserLists.Clear();

            foreach (var list in taskLists)
            {
                _taskListsBackingStore.Add(list);

                var icon = _iconStorageService.GetIcon(list.Id);
                var navItem = new NavItem
                {
                    Id = list.Id,
                    Title = list.Title,
                    Icon = icon,
                    Type = NavItemType.UserList,
                    Data = list
                };

                if (navItem.Id == selectedId)
                {
                    navItem.IsSelected = true;
                    SelectedNavItem = navItem;
                }

                UserLists.Add(navItem);
            }

            // Refresh current view if a list is selected
            await RefreshCurrentViewAsync();

            LastSyncTime = DateTimeOffset.Now;
            OrbStatusChanged?.Invoke(OrbStatusKind.Connected);
            StatusText = GetResource("ShellStatusSynced", "Synced");
            RippleRequested?.Invoke();
        }
        catch (HttpRequestException)
        {
            // Network error - silent failure for background sync
            OrbStatusChanged?.Invoke(OrbStatusKind.Offline);
            StatusText = GetResource("ShellStatusOffline", "Offline");
        }
        catch (AuthenticationExpiredException)
        {
            await HandleAuthenticationExpiredAsync();
        }
        catch (Exception ex)
        {
            // Unexpected error - log but don't interrupt user
            OrbStatusChanged?.Invoke(OrbStatusKind.Warning);
            StatusText = GetResource("ShellStatusSyncFailed", "Sync failed");
            System.Diagnostics.Debug.WriteLine($"[AutoSync] Error: {ex}");
        }
        finally
        {
            _isSyncing = false;
        }
    }

    /// <summary>
    /// Performs the first sync after app launch and clears the initial loading state.
    /// </summary>
    private async Task PerformInitialSyncAsync()
    {
        try
        {
            await PerformAutoSyncAsync();
        }
        finally
        {
            IsInitialLoading = false;
        }
    }

    /// <summary>
    /// Refreshes the tasks for the currently selected view.
    /// </summary>
    private async Task RefreshCurrentViewAsync()
    {
        if (SelectedNavItem is null || TaskListVM is null)
            return;

        try
        {
            if (SelectedNavItem.Type == NavItemType.UserList && SelectedNavItem.Data is TaskList selectedList)
            {
                // Refresh user list
                var tasks = await _taskService.GetTasksAsync(selectedList.Id);
                TaskListVM.LoadTasks(tasks.ToList(), TaskListVM.CurrentSort, TaskListVM.CurrentFilter);
            }
        }
        catch (HttpRequestException)
        {
            OrbStatusChanged?.Invoke(OrbStatusKind.Offline);
            StatusText = GetResource("ShellStatusOffline", "Offline");
        }
        catch (Exception ex)
        {
            OrbStatusChanged?.Invoke(OrbStatusKind.Warning);
            System.Diagnostics.Debug.WriteLine($"[RefreshCurrentView] Error: {ex}");
        }
    }

    /// <summary>
    /// Schedules an automatic sync after a user-initiated data change.
    /// Uses debouncing to prevent sync spam during rapid edits.
    /// </summary>
    private void ScheduleSyncAfterChange()
    {
        if (!_isAutoSyncEnabled)
            return;

        // Reset debounce timer (2 seconds after last change)
        if (_debounceTimer is not null)
        {
            _debounceTimer.Stop();
        }
        else
        {
            _debounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _debounceTickHandler = async (s, e) =>
            {
                _debounceTimer?.Stop();
                await PerformAutoSyncAsync();
            };
            _debounceTimer.Tick += _debounceTickHandler;
        }

        _debounceTimer.Start();
    }

    // --- Authentication Recovery ---

    /// <summary>
    /// Handles expired authentication by showing a dialog and re-authenticating if the user agrees.
    /// </summary>
    private async Task HandleAuthenticationExpiredAsync()
    {
        OrbStatusChanged?.Invoke(OrbStatusKind.Warning);
        StatusText = GetResource("ShellStatusAuthExpired", "Session expired");

        var confirmed = await _dialogService.ShowConfirmationAsync(
            GetResource("AuthExpiredDialogTitle", "Session Expired"),
            GetResource("AuthExpiredDialogMessage", "Your Google sign-in has expired. Would you like to sign in again?"),
            GetResource("AuthExpiredDialogPrimary", "Sign in"),
            GetResource("AuthExpiredDialogClose", "Cancel"));

        if (!confirmed)
            return;

        try
        {
            StatusText = GetResource("ShellStatusReAuthenticating", "Signing in...");
            OrbStatusChanged?.Invoke(OrbStatusKind.Syncing);

            await _authService.ReAuthenticateAsync();

            // Retry sync after re-authentication
            await PerformAutoSyncAsync();
        }
        catch (Exception ex)
        {
            OrbStatusChanged?.Invoke(OrbStatusKind.Offline);
            StatusText = GetResource("ShellStatusSyncFailed", "Sync failed");
            ShowError(string.Format(GetResource("ShellStatusSyncErrorFormat", "Sync error: {0}"), ex.Message));
        }
    }

    // --- Status helpers ---

    private void ShowSuccess(string message)
    {
        // If we're currently offline but an operation succeeds, we're back online
        if (StatusText == GetResource("ShellStatusOffline", "Offline") ||
            StatusText == GetResource("ShellStatusSyncFailed", "Sync failed"))
        {
            OrbStatusChanged?.Invoke(OrbStatusKind.Connected);
            StatusText = GetResource("ShellStatusConnected", "Connected");
        }

        RippleRequested?.Invoke();
        TemporaryStatusRequested?.Invoke(message);
    }

    private void ShowError(string message)
    {
        OrbStatusChanged?.Invoke(OrbStatusKind.Offline);
        StatusText = GetResource("ShellStatusOffline", "Offline");
        TemporaryStatusRequested?.Invoke(message);
    }

    private void ShowWarning(string message)
    {
        OrbStatusChanged?.Invoke(OrbStatusKind.Warning);
        TemporaryStatusRequested?.Invoke(message);
    }

    private void ShowInfo(string message)
    {
        TemporaryStatusRequested?.Invoke(message);
    }

    private string GetResource(string key, string fallback)
    {
        var value = _resourceLoader.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}

/// <summary>
/// Orb status values communicated from ViewModel to View.
/// Mirrors the control enum to avoid ViewModel depending on UI controls.
/// </summary>
public enum OrbStatusKind
{
    Connected,
    Syncing,
    Warning,
    Offline
}

/// <summary>
/// Describes a status message raised by a child ViewModel.
/// </summary>
public sealed class StatusMessageEventArgs(StatusKind kind, string message) : EventArgs
{
    public StatusKind Kind { get; } = kind;
    public string Message { get; } = message;
}

/// <summary>
/// Kind of status message.
/// </summary>
public enum StatusKind
{
    Success,
    Warning,
    Error,
    Info
}
