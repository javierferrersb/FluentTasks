using Microsoft.UI.Xaml.Data;
using System;

namespace FluentTasks.UI.Converters;

public class DateToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTimeOffset date)
        {
            var now = DateTimeOffset.Now;
            var daysUntil = (date.Date - now.Date).Days;

            if (daysUntil < 0)
            {
                var daysOverdue = Math.Abs(daysUntil);
                return daysOverdue == 1
                    ? "Overdue by 1 day"
                    : $"Overdue by {daysOverdue} days";
            }
            else if (daysUntil == 0)
                return "Due Today";
            else if (daysUntil == 1)
                return "Due Tomorrow";
            else if (daysUntil <= 7)
                return $"Due {date.ToString("dddd")}";
            else
                return $"Due {date.ToString("MMM d")}";
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}