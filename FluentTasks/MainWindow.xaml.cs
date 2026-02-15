using System.ComponentModel;
using FluentTasks.Core.Models;
using FluentTasks.UI.Controls;
using FluentTasks.UI.Models;
using FluentTasks.UI.Services;
using FluentTasks.UI.ViewModels;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FluentTasks.UI;

public sealed partial class MainWindow : Window
{
    public ShellViewModel ViewModel { get; }

    private TaskItem? _draggedTask;

    public MainWindow()
    {
        this.InitializeComponent();

        ViewModel = App.GetService<ShellViewModel>();

        // Wire up ViewModel events for view-specific effects
        ViewModel.RippleRequested += () => StatusOrb.TriggerRipple();
        ViewModel.OrbStatusChanged += kind => StatusOrb.SetStatus(kind switch
        {
            OrbStatusKind.Connected => OrbStatus.Connected,
            OrbStatusKind.Syncing => OrbStatus.Syncing,
            OrbStatusKind.Warning => OrbStatus.Warning,
            OrbStatusKind.Offline => OrbStatus.Offline,
            _ => OrbStatus.Connected
        });
        ViewModel.TemporaryStatusRequested += ShowTemporaryStatus;

        // Sync VM property changes to UI elements
        ViewModel.PropertyChanged += ViewModel_PropertyChanged;

        // Set up custom title bar
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;

        // Initialize the dialog service with the XamlRoot once content is loaded
        if (this.Content is FrameworkElement root)
        {
            root.Loaded += (_, _) =>
            {
                var dialogService = App.GetService<DialogService>();
                dialogService.SetXamlRoot(root.XamlRoot);
            };
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(ShellViewModel.IsNoListSelected):
                NoListSelectedState.Visibility = ViewModel.IsNoListSelected ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(ShellViewModel.IsTaskContentVisible):
                TaskContentArea.Visibility = ViewModel.IsTaskContentVisible ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(ShellViewModel.StatusText):
                StatusText.Text = ViewModel.StatusText;
                break;
            case nameof(ShellViewModel.TaskListVM):
                WireTaskListVM();
                break;
        }
    }

    private void WireTaskListVM()
    {
        if (ViewModel.TaskListVM is null) return;

        ViewModel.TaskListVM.PropertyChanged += TaskListVM_PropertyChanged;
        SyncTaskListVMState();
    }

    private void TaskListVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TaskListViewModel.Title):
                TaskListTitle.Text = ViewModel.TaskListVM!.Title;
                break;
            case nameof(TaskListViewModel.Tasks):
                TasksView.ItemsSource = ViewModel.TaskListVM!.Tasks;
                break;
            case nameof(TaskListViewModel.IsEmpty):
                EmptyState.Visibility = ViewModel.TaskListVM!.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(TaskListViewModel.IsLoading):
                SkeletonLoadingState.Visibility = ViewModel.TaskListVM!.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(TaskListViewModel.ShowAddTaskInput):
                AddTaskInputGrid.Visibility = ViewModel.TaskListVM!.ShowAddTaskInput ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(TaskListViewModel.SortButtonText):
                SortButtonText.Text = ViewModel.TaskListVM!.SortButtonText;
                SortButtonText.Foreground = ViewModel.TaskListVM.IsSortActive
                    ? (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
                    : (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
                break;
            case nameof(TaskListViewModel.FilterButtonText):
                FilterButtonText.Text = ViewModel.TaskListVM!.FilterButtonText;
                FilterButtonText.Foreground = ViewModel.TaskListVM.IsFilterActive
                    ? (Brush)Application.Current.Resources["AccentTextFillColorPrimaryBrush"]
                    : (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"];
                break;
            case nameof(TaskListViewModel.NewTaskTitle):
                if (NewTaskInput.Text != ViewModel.TaskListVM!.NewTaskTitle)
                    NewTaskInput.Text = ViewModel.TaskListVM.NewTaskTitle;
                break;
        }
    }

    private void SyncTaskListVMState()
    {
        if (ViewModel.TaskListVM is null) return;

        TaskListTitle.Text = ViewModel.TaskListVM.Title;
        TasksView.ItemsSource = ViewModel.TaskListVM.Tasks;
        EmptyState.Visibility = ViewModel.TaskListVM.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        SkeletonLoadingState.Visibility = ViewModel.TaskListVM.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        AddTaskInputGrid.Visibility = ViewModel.TaskListVM.ShowAddTaskInput ? Visibility.Visible : Visibility.Collapsed;
        SortButtonText.Text = ViewModel.TaskListVM.SortButtonText;
        FilterButtonText.Text = ViewModel.TaskListVM.FilterButtonText;
    }

    // --- Navigation ---

    private void MenuItem_Clicked(object sender, EventArgs e)
    {
        if (sender is MenuItemControl control && control.Tag is NavItem navItem)
        {
            _ = ViewModel.SelectNavItemAsync(navItem);
        }
    }

    private void MenuItem_EditClicked(object sender, EventArgs e)
    {
        if (sender is MenuItemControl control && control.Tag is NavItem navItem)
        {
            _ = ViewModel.RenameListAsync(navItem);
        }
    }

    private void MenuItem_DeleteClicked(object sender, EventArgs e)
    {
        if (sender is MenuItemControl control && control.Tag is NavItem navItem)
        {
            _ = ViewModel.DeleteListAsync(navItem);
        }
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Implement settings page later
    }

    // --- Sync ---

    private async void LoadTaskLists_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.SyncCommand.ExecuteAsync(null);
    }

    // --- Create List ---

    private async void CreateList_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.CreateListCommand.ExecuteAsync(null);
    }

    // --- Task actions (delegate to TaskListVM) ---

    private async void AddTask_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.TaskListVM is not null)
        {
            ViewModel.TaskListVM.NewTaskTitle = NewTaskInput.Text;
            await ViewModel.TaskListVM.AddTaskCommand.ExecuteAsync(null);
            NewTaskInput.Text = ViewModel.TaskListVM.NewTaskTitle;
        }
    }

    private async void AddTask_Enter(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (ViewModel.TaskListVM is not null)
        {
            ViewModel.TaskListVM.NewTaskTitle = NewTaskInput.Text;
            await ViewModel.TaskListVM.AddTaskCommand.ExecuteAsync(null);
            NewTaskInput.Text = ViewModel.TaskListVM.NewTaskTitle;
        }
        args.Handled = true;
    }

    private async void TaskCheckbox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkbox || checkbox.Tag is not TaskItem task)
            return;

        if (ViewModel.TaskListVM is null)
            return;

        var newCompletedState = checkbox.IsChecked == true;
        var success = await ViewModel.TaskListVM.CompleteTaskAsync(task, newCompletedState);

        if (!success)
        {
            checkbox.IsChecked = !newCompletedState;
        }
    }

    private async void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TaskItem task)
            return;

        if (ViewModel.TaskListVM is not null)
            await ViewModel.TaskListVM.DeleteTaskAsync(task);
    }

    private void TaskTitle_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock || textBlock.Tag is not TaskItem task)
            return;

        ViewModel.TaskListVM?.BeginEditTask(task);

        // Focus the textbox after a short delay to let UI update
        DispatcherQueue.TryEnqueue(async () =>
        {
            await Task.Delay(50);
            FocusEditTextBox(task);
        });
    }

    private async void SaveEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TaskItem task })
            await (ViewModel.TaskListVM?.SaveEditAsync(task) ?? Task.CompletedTask);
    }

    private async void SaveEdit_Enter(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.Element is FrameworkElement { Tag: TaskItem task })
            await (ViewModel.TaskListVM?.SaveEditAsync(task) ?? Task.CompletedTask);
        args.Handled = true;
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TaskItem task })
            ViewModel.TaskListVM?.CancelEdit(task);
    }

    private void CancelEdit_Escape(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.Element is FrameworkElement { Tag: TaskItem task })
            ViewModel.TaskListVM?.CancelEdit(task);
        args.Handled = true;
    }

    private async void TaskDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TaskItem task)
            return;

        if (ViewModel.TaskListVM is not null)
            await ViewModel.TaskListVM.ShowDetailsAsync(task);
    }

    private async void AddSubtask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TaskItem parentTask)
            return;

        if (ViewModel.TaskListVM is not null)
            await ViewModel.TaskListVM.AddSubtaskAsync(parentTask);
    }

    private async void DateChip_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not TaskItem task)
            return;

        if (ViewModel.TaskListVM is not null)
            await ViewModel.TaskListVM.ShowDetailsAsync(task);
    }

    // --- Sort / Filter ---

    private void SortMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item &&
            Enum.TryParse<SortOption>(item.Tag?.ToString(), out var sortOption))
        {
            ViewModel.TaskListVM?.SetSort(sortOption);

            // Update checkmarks in the menu
            SortDefault.Icon = null;
            SortDueDateAsc.Icon = null;
            SortDueDateDesc.Icon = null;
            SortAlpha.Icon = null;
            SortAlphaRev.Icon = null;
            SortCompleted.Icon = null;
            item.Icon = new FontIcon { Glyph = "\uE73E" };
        }
    }

    private void FilterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item &&
            Enum.TryParse<FilterOption>(item.Tag?.ToString(), out var filterOption))
        {
            ViewModel.TaskListVM?.SetFilter(filterOption);

            // Update checkmarks in the menu
            FilterAll.Icon = null;
            FilterIncomplete.Icon = null;
            FilterCompleted.Icon = null;
            FilterOverdue.Icon = null;
            FilterToday.Icon = null;
            FilterWeek.Icon = null;
            item.Icon = new FontIcon { Glyph = "\uE73E" };
        }
    }

    private void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel.TaskListVM?.UpdateSearch(sender.Text);
        }
    }

    // --- Drag and drop (view-specific UI wiring) ---

    private void Task_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        if (sender is Border border && border.Tag is TaskItem task)
        {
            _draggedTask = task;
            args.Data.RequestedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            args.Data.SetText(task.Title);
        }
    }

    private void Task_DragOver(object sender, DragEventArgs e)
    {
        if (_draggedTask == null)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            return;
        }

        if (sender is Border border && border.Tag is TaskItem targetTask)
        {
            if (!TaskListViewModel.IsValidDropTarget(_draggedTask, targetTask))
            {
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
                return;
            }

            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
            e.DragUIOverride.IsCaptionVisible = false;
            e.DragUIOverride.IsGlyphVisible = false;
        }
    }

    private async void Task_Drop(object sender, DragEventArgs e)
    {
        if (_draggedTask == null)
            return;

        if (sender is Border border && border.Tag is TaskItem targetTask)
        {
            if (ViewModel.TaskListVM is not null)
                await ViewModel.TaskListVM.MoveTaskAsync(_draggedTask, targetTask);
        }

        _draggedTask = null;
    }

    // --- View-specific UI helpers ---

    private void TaskCard_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["CardBackgroundFillColorDefaultBrush"];
        }
    }

    private void TaskCard_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            border.Background = (Brush)Application.Current.Resources["LayerFillColorDefaultBrush"];
        }
    }

    private void TaskTitle_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            textBlock.TextDecorations = Windows.UI.Text.TextDecorations.Underline;
        }
    }

    private void TaskTitle_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            textBlock.TextDecorations = Windows.UI.Text.TextDecorations.None;
        }
    }

    private void FocusEditTextBox(TaskItem task)
    {
        if (TasksView.ItemsSource is IEnumerable<TaskItem> items)
        {
            var index = items.ToList().IndexOf(task);
            if (index >= 0)
            {
                var container = TasksView.TryGetElement(index);
                if (container != null)
                {
                    var textBox = FindTextBoxInVisualTree(container);
                    if (textBox != null)
                    {
                        textBox.Focus(FocusState.Programmatic);
                        textBox.SelectAll();
                    }
                }
            }
        }
    }

    private static TextBox? FindTextBoxInVisualTree(DependencyObject parent)
    {
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is TextBox textBox && textBox.Tag is TaskItem)
                return textBox;

            var result = FindTextBoxInVisualTree(child);
            if (result != null)
                return result;
        }
        return null;
    }

    private async void ShowTemporaryStatus(string message)
    {
        ActionText.Text = $"• {message}";

        var fadeInStoryboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        var fadeIn = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase
            {
                EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseOut
            }
        };

        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeIn, ActionText);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeIn, "Opacity");
        fadeInStoryboard.Children.Add(fadeIn);
        fadeInStoryboard.Begin();

        await System.Threading.Tasks.Task.Delay(3000);

        var fadeOutStoryboard = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
        var fadeOut = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = new Duration(TimeSpan.FromMilliseconds(300)),
            EasingFunction = new Microsoft.UI.Xaml.Media.Animation.CubicEase
            {
                EasingMode = Microsoft.UI.Xaml.Media.Animation.EasingMode.EaseIn
            }
        };

        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(fadeOut, ActionText);
        Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(fadeOut, "Opacity");
        fadeOutStoryboard.Children.Add(fadeOut);
        fadeOutStoryboard.Begin();
    }
}
