using FluentTasks.Core.Models;
using FluentTasks.UI.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace FluentTasks.UI.Controls;

/// <summary>
/// Displays the task list content area including sort/filter, task items, and status bar.
/// Bind <see cref="ViewModel"/> to supply the active <see cref="TaskListViewModel"/>.
/// </summary>
public sealed partial class TaskListControl : UserControl
{
    /// <summary>
    /// Backing store for <see cref="ViewModel"/>.
    /// </summary>
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(TaskListViewModel),
            typeof(TaskListControl),
            new PropertyMetadata(null, OnViewModelChanged));

    /// <summary>
    /// Backing store for <see cref="ShowHamburgerButton"/>.
    /// </summary>
    public static readonly DependencyProperty ShowHamburgerButtonProperty =
        DependencyProperty.Register(
            nameof(ShowHamburgerButton),
            typeof(bool),
            typeof(TaskListControl),
            new PropertyMetadata(false, OnShowHamburgerButtonChanged));

    private TaskItem? _draggedTask;

    public TaskListControl()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// The active task-list view model. When set, the control subscribes to property
    /// changes and keeps the UI in sync.
    /// </summary>
    public TaskListViewModel? ViewModel
    {
        get => (TaskListViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    /// <summary>
    /// Whether to show the hamburger menu button in the header.
    /// </summary>
    public bool ShowHamburgerButton
    {
        get => (bool)GetValue(ShowHamburgerButtonProperty);
        set => SetValue(ShowHamburgerButtonProperty, value);
    }

    /// <summary>
    /// Raised when the hamburger menu button is clicked.
    /// </summary>
    public event EventHandler? HamburgerButtonClicked;

    /// <summary>
    /// The StatusOrb control exposed so the parent can drive ripple / status animations.
    /// </summary>
    public StatusOrb Orb => StatusOrb;

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (TaskListControl)d;

        if (e.OldValue is TaskListViewModel oldVm)
        {
            oldVm.PropertyChanged -= control.ViewModel_PropertyChanged;
        }

        if (e.NewValue is TaskListViewModel newVm)
        {
            newVm.PropertyChanged += control.ViewModel_PropertyChanged;
            control.SyncAllState();
        }
    }

    private static void OnShowHamburgerButtonChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TaskListControl control)
        {
            control.HamburgerButton.Visibility = (bool)e.NewValue ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void HamburgerButton_Click(object sender, RoutedEventArgs e)
    {
        HamburgerButtonClicked?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Shows the task content area and hides the empty placeholder.
    /// </summary>
    public void ShowTaskContent()
    {
        NoListSelectedState.Visibility = Visibility.Collapsed;
        TaskContentArea.Visibility = Visibility.Visible;
        TaskListTitle.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// Hides the task content area and shows the empty placeholder.
    /// </summary>
    public void ShowEmptyPlaceholder()
    {
        TaskContentArea.Visibility = Visibility.Collapsed;
        NoListSelectedState.Visibility = Visibility.Visible;
        TaskListTitle.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Updates the status text.
    /// </summary>
    public void SetStatusText(string text) => StatusText.Text = text;

    /// <summary>
    /// Plays the temporary action text fade-in / fade-out animation.
    /// </summary>
    public async void ShowTemporaryStatus(string message)
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

    // --- ViewModel property sync ---

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(TaskListViewModel.Title):
                TaskListTitle.Text = ViewModel!.Title;
                break;
            case nameof(TaskListViewModel.Tasks):
                TasksView.ItemsSource = ViewModel!.Tasks;
                break;
            case nameof(TaskListViewModel.IsEmpty):
                EmptyState.Visibility = ViewModel!.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(TaskListViewModel.IsLoading):
                SkeletonLoadingState.Visibility = ViewModel!.IsLoading ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(TaskListViewModel.ShowAddTaskInput):
                AddTaskInputGrid.Visibility = ViewModel!.ShowAddTaskInput ? Visibility.Visible : Visibility.Collapsed;
                break;
            case nameof(TaskListViewModel.SortButtonText):
                SortButtonText.Text = ViewModel!.SortButtonText;
                SortButtonText.Foreground = ViewModel.IsSortActive
                    ? GetThemeResource<Brush>("AccentTextFillColorPrimaryBrush")
                    : GetThemeResource<Brush>("TextFillColorSecondaryBrush");
                break;
            case nameof(TaskListViewModel.FilterButtonText):
                FilterButtonText.Text = ViewModel!.FilterButtonText;
                FilterButtonText.Foreground = ViewModel.IsFilterActive
                    ? GetThemeResource<Brush>("AccentTextFillColorPrimaryBrush")
                    : GetThemeResource<Brush>("TextFillColorSecondaryBrush");
                break;
            case nameof(TaskListViewModel.NewTaskTitle):
                if (NewTaskInput.Text != ViewModel!.NewTaskTitle)
                    NewTaskInput.Text = ViewModel.NewTaskTitle;
                break;
            case nameof(TaskListViewModel.CurrentSort):
                SyncSortCheckedState();
                break;
            case nameof(TaskListViewModel.CurrentFilter):
                SyncFilterCheckedState();
                break;
            case nameof(TaskListViewModel.EmptyStateIcon):
            case nameof(TaskListViewModel.EmptyStateTitle):
            case nameof(TaskListViewModel.EmptyStateSubtitle):
                SyncEmptyStateText();
                break;
        }
    }

    private void SyncAllState()
    {
        if (ViewModel is null) return;

        TaskListTitle.Text = ViewModel.Title;
        TasksView.ItemsSource = ViewModel.Tasks;
        EmptyState.Visibility = ViewModel.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
        SkeletonLoadingState.Visibility = ViewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        AddTaskInputGrid.Visibility = ViewModel.ShowAddTaskInput ? Visibility.Visible : Visibility.Collapsed;
        SortButtonText.Text = ViewModel.SortButtonText;
        FilterButtonText.Text = ViewModel.FilterButtonText;

        // Set sort button colors
        var sortForeground = ViewModel.IsSortActive
            ? GetThemeResource<Brush>("TextFillColorPrimaryBrush")
            : GetThemeResource<Brush>("AccentTextFillColorSecondaryBrush");
        SortButtonText.Foreground = sortForeground;
        SortButtonIcon.Foreground = sortForeground;

        // Set filter button colors
        var filterForeground = ViewModel.IsFilterActive
            ? GetThemeResource<Brush>("TextFillColorPrimaryBrush")
            : GetThemeResource<Brush>("AccentTextFillColorSecondaryBrush");
        FilterButtonText.Foreground = filterForeground;
        FilterButtonIcon.Foreground = filterForeground;

        SyncSortCheckedState();
        SyncFilterCheckedState();
        SyncEmptyStateText();
    }

    private void SyncSortCheckedState()
    {
        if (ViewModel is null) return;

        SortDefault.IsChecked = ViewModel.CurrentSort == SortOption.None;
        SortDueDateAsc.IsChecked = ViewModel.CurrentSort == SortOption.DueDateAscending;
        SortDueDateDesc.IsChecked = ViewModel.CurrentSort == SortOption.DueDateDescending;
        SortAlpha.IsChecked = ViewModel.CurrentSort == SortOption.Alphabetical;
        SortAlphaRev.IsChecked = ViewModel.CurrentSort == SortOption.AlphabeticalReverse;
        SortCompleted.IsChecked = ViewModel.CurrentSort == SortOption.CompletedLast;
    }

    private void SyncFilterCheckedState()
    {
        if (ViewModel is null) return;

        FilterAll.IsChecked = ViewModel.CurrentFilter == FilterOption.All;
        FilterIncomplete.IsChecked = ViewModel.CurrentFilter == FilterOption.Incomplete;
        FilterCompleted.IsChecked = ViewModel.CurrentFilter == FilterOption.Completed;
        FilterOverdue.IsChecked = ViewModel.CurrentFilter == FilterOption.Overdue;
        FilterToday.IsChecked = ViewModel.CurrentFilter == FilterOption.Today;
        FilterWeek.IsChecked = ViewModel.CurrentFilter == FilterOption.ThisWeek;
    }

    private void SyncEmptyStateText()
    {
        if (ViewModel is null) return;

        EmptyStateIcon.Text = ViewModel.EmptyStateIcon;
        EmptyStateTitle.Text = ViewModel.EmptyStateTitle;
        EmptyStateSubtitle.Text = ViewModel.EmptyStateSubtitle;
    }

    // --- Add task ---

    private async void AddTask_Click(object sender, RoutedEventArgs e)
    {
        await AddTaskAsync();
    }

    private async void AddTask_Enter(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        await AddTaskAsync();
        args.Handled = true;
    }

    private async Task AddTaskAsync()
    {
        if (ViewModel is null) return;

        ViewModel.NewTaskTitle = NewTaskInput.Text;
        await ViewModel.AddTaskCommand.ExecuteAsync(null);
        NewTaskInput.Text = ViewModel.NewTaskTitle;
    }

    // --- Checkbox ---

    private async void TaskCheckbox_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkbox || checkbox.Tag is not TaskItem task)
            return;
        if (ViewModel is null) return;

        var newCompletedState = checkbox.IsChecked == true;
        var success = await ViewModel.CompleteTaskAsync(task, newCompletedState);

        if (!success)
        {
            checkbox.IsChecked = !newCompletedState;
        }
    }

    // --- Delete ---

    private async void DeleteTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TaskItem task) return;
        if (ViewModel is not null)
            await ViewModel.DeleteTaskAsync(task);
    }

    // --- Edit inline ---

    private void TaskTitle_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock) return;

        // Only handle left button clicks
        var pointerPoint = e.GetCurrentPoint(textBlock);
        if (pointerPoint.Properties.IsLeftButtonPressed == false) return;

        // Try to get the task from Tag first, then from DataContext
        TaskItem? task = textBlock.Tag as TaskItem ?? textBlock.DataContext as TaskItem;
        if (task is null || ViewModel is null) return;

        // Mark the event as handled to prevent drag operations
        e.Handled = true;

        ViewModel.BeginEditTask(task);

        DispatcherQueue.TryEnqueue(async () =>
        {
            await Task.Delay(100);
            FocusEditTextBox(task);
        });
    }

    private async void SaveEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TaskItem task })
            await (ViewModel?.SaveEditAsync(task) ?? Task.CompletedTask);
    }

    private async void SaveEdit_Enter(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.Element is FrameworkElement { Tag: TaskItem task })
            await (ViewModel?.SaveEditAsync(task) ?? Task.CompletedTask);
        args.Handled = true;
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: TaskItem task })
            ViewModel?.CancelEdit(task);
    }

    private void CancelEdit_Escape(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.Element is FrameworkElement { Tag: TaskItem task })
            ViewModel?.CancelEdit(task);
        args.Handled = true;
    }

    // --- Details / Subtask ---

    private async void TaskDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TaskItem task) return;
        if (ViewModel is not null)
            await ViewModel.ShowDetailsAsync(task);
    }

    private async void AddSubtask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not TaskItem parentTask) return;
        if (ViewModel is not null)
            await ViewModel.AddSubtaskAsync(parentTask);
    }

    private async void DateChip_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is not Border border || border.Tag is not TaskItem task) return;
        if (ViewModel is not null)
            await ViewModel.ShowDetailsAsync(task);
    }

    // --- Sort / Filter / Search ---

    private void SortMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem item &&
            Enum.TryParse<SortOption>(item.Tag?.ToString(), out var sortOption))
        {
            ViewModel?.SetSort(sortOption);
        }
    }

    private void FilterMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioMenuFlyoutItem item &&
            Enum.TryParse<FilterOption>(item.Tag?.ToString(), out var filterOption))
        {
            ViewModel?.SetFilter(filterOption);
        }
    }

    private void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        {
            ViewModel?.UpdateSearch(sender.Text);
        }
    }

    // --- Drag and drop ---

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
        if (_draggedTask is null)
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
        if (_draggedTask is null) return;

        if (sender is Border border && border.Tag is TaskItem targetTask)
        {
            if (ViewModel is not null)
                await ViewModel.MoveTaskAsync(_draggedTask, targetTask);
        }

        _draggedTask = null;
    }

    // --- Pointer effects ---

    private void TaskCard_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            // Use SubtleFillColorSecondary for a subtle but visible hover effect
            border.Background = GetThemeResource<Brush>("SubtleFillColorSecondaryBrush");
        }
    }

    private void TaskCard_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is Border border)
        {
            // Clear the local value to let the style's ThemeResource take over
            border.ClearValue(Border.BackgroundProperty);
        }
    }

    private T? GetThemeResource<T>(string resourceKey) where T : class
    {
        // Try to get resource from XamlRoot first (respects current theme)
        if (this.XamlRoot?.Content is FrameworkElement root)
        {
            if (root.Resources.TryGetValue(resourceKey, out var resource))
                return resource as T;
        }

        // Fallback to application resources
        if (Application.Current.Resources.TryGetValue(resourceKey, out var appResource))
            return appResource as T;

        return null;
    }

    private void TaskTitle_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
            textBlock.TextDecorations = Windows.UI.Text.TextDecorations.Underline;
    }

    private void TaskTitle_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
            textBlock.TextDecorations = Windows.UI.Text.TextDecorations.None;
    }

    // --- Focus helpers ---

    private void FocusEditTextBox(TaskItem task)
    {
        if (TasksView.ItemsSource is IEnumerable<TaskItem> items)
        {
            var index = items.ToList().IndexOf(task);
            if (index >= 0)
            {
                var container = TasksView.TryGetElement(index);
                if (container is not null)
                {
                    var textBox = FindTextBoxInVisualTree(container);
                    if (textBox is not null)
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
            if (result is not null)
                return result;
        }
        return null;
    }

    private void ThisControl_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Force VisualStateManager to re-evaluate based on control width
        // This is handled automatically via AdaptiveTrigger on window width
    }
}
