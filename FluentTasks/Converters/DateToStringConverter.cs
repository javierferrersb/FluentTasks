using Microsoft.UI.Xaml.Data;
using System;

namespace FluentTasks.UI.Converters;

public class DateToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTimeOffset date)
        {
            var daysUntil = (date.Date - DateTimeOffset.Now.Date).Days;

            if (daysUntil < 0)
                return $"📅 Overdue";
            else if (daysUntil == 0)
                return $"📅 Today";
            else if (daysUntil == 1)
                return $"📅 Tomorrow";
            else if (daysUntil <= 7)
                return $"📅 {date.ToString("dddd")}"; // Day name
            else
                return $"📅 {date.ToString("MMM d")}"; // e.g., "Jan 15"
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}