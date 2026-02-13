using FluentTasks.Core.Models;
using Microsoft.UI.Xaml.Controls;
using System;

namespace FluentTasks.UI.Dialogs;

public sealed partial class TaskDetailsDialog : ContentDialog
{
    public TaskItem Task { get; }

    // Temporary values for editing (not bound to task until Save)
    private string _originalTitle;
    private DateTimeOffset? _originalDueDate;
    private DateTimeOffset? _tempDueDate;

    public TaskDetailsDialog(TaskItem task)
    {
        Task = task;

        // Save original values
        _originalTitle = task.Title;
        _originalDueDate = task.DueDate;
        _tempDueDate = task.DueDate;

        this.InitializeComponent();

        // Set initial values
        TitleTextBox.Text = task.Title;
        DueDatePicker.Date = task.DueDate;

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
            StatusInfo.Text = "Completed";
        }
        else if (daysUntilDue < 0)
        {
            StatusInfo.Text = $"Overdue by {Math.Abs(daysUntilDue)} days";
            StatusInfo.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Microsoft.UI.Colors.Red);
        }
        else if (daysUntilDue == 0)
        {
            StatusInfo.Text = "Due today";
        }
        else if (daysUntilDue == 1)
        {
            StatusInfo.Text = "Due tomorrow";
        }
        else
        {
            StatusInfo.Text = $"Due in {daysUntilDue} days";
        }
    }
}