using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace FluentTasks.UI.Converters;

/// <summary>
/// Returns margin for subtasks in default sort, no margin otherwise
/// </summary>
public class SubtaskMarginConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isSubtask && isSubtask)
        {
            return new Thickness(32, 0, 0, 0);
        }
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}