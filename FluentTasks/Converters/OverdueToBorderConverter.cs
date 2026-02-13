using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace FluentTasks.UI.Converters;

public class OverdueToBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isOverdue && isOverdue)
        {
            // Red border for overdue
            return new SolidColorBrush(Colors.Red);
        }

        // Same as subtask chip - accent border
        return Microsoft.UI.Xaml.Application.Current.Resources["AccentFillColorSecondaryBrush"] as SolidColorBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}