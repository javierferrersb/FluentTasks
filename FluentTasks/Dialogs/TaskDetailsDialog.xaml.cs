using FluentTasks.Core.Models;
using Microsoft.UI.Xaml.Controls;
using System;
using Microsoft.Windows.ApplicationModel.Resources;

namespace FluentTasks.UI.Dialogs;

public sealed partial class TaskDetailsDialog : ContentDialog
{
    public TaskItem Task { get; }

    // Temporary values for editing (not bound to task until Save)
    private string _originalTitle;
    private DateTimeOffset? _originalDueDate;
    private DateTimeOffset? _tempDueDate;
    private readonly ResourceLoader _resourceLoader;

    public TaskDetailsDialog(TaskItem task)
    {
        Task = task;
        _resourceLoader = new ResourceLoader();

        // Save original values
        _originalTitle = task.Title;
        _originalDueDate = task.DueDate;
        _tempDueDate = task.DueDate;

        this.InitializeComponent();

        // Set initial values
        TitleTextBox.Text = task.Title;
        DueDatePicker.Date = task.DueDate;
        NotesTextBox.Text = task.Notes ?? "";

        UpdateStatusInfo();

        // Listen for date changes
        DueDatePicker.DateChanged += DueDatePicker_DateChanged;
    }

    private void DueDatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        _tempDueDate = args.NewDate;
        UpdateStatusInfo();
    }

    private void Save_Click(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(TitleTextBox.Text))
        {
            args.Cancel = true;
            return;
        }

        // Apply changes to the actual task
        Task.Title = TitleTextBox.Text.Trim();
        Task.DueDate = _tempDueDate;
        Task.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text)
        ? null
        : NotesTextBox.Text.Trim();
    }

    private void ClearDueDate_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _tempDueDate = null;
        DueDatePicker.Date = null;
        UpdateStatusInfo();
    }

    private void UpdateStatusInfo()
    {
        if (!_tempDueDate.HasValue)
        {
            StatusInfo.Text = "";
            return;
        }

        var daysUntilDue = (_tempDueDate.Value.Date - DateTimeOffset.Now.Date).Days;

        if (Task.IsCompleted)
        {
            StatusInfo.Text = _resourceLoader.GetString("TaskDetailsStatusCompleted");
        }
        else if (daysUntilDue < 0)
        {
            StatusInfo.Text = string.Format(_resourceLoader.GetString("TaskDetailsStatusOverdue"), Math.Abs(daysUntilDue));
            StatusInfo.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Red);
        }
        else if (daysUntilDue == 0)
        {
            StatusInfo.Text = _resourceLoader.GetString("TaskDetailsStatusDueToday");
        }
        else if (daysUntilDue == 1)
        {
            StatusInfo.Text = _resourceLoader.GetString("TaskDetailsStatusDueTomorrow");
        }
        else
        {
            StatusInfo.Text = string.Format(_resourceLoader.GetString("TaskDetailsStatusDueInDays"), daysUntilDue);
        }
    }
}