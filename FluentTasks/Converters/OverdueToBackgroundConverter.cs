using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace FluentTasks.UI.Converters;

public class OverdueToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isOverdue && isOverdue)
        {
            // Red tinted background for overdue
            return new SolidColorBrush(Color.FromArgb(40, 255, 0, 0)); // Light red
        }

        // Same as subtask chip - subtle background
        return Microsoft.UI.Xaml.Application.Current.Resources["SubtleFillColorSecondaryBrush"] as SolidColorBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}