using Microsoft.UI.Xaml.Data;
using Microsoft.Windows.ApplicationModel.Resources;
using System;

namespace FluentTasks.UI.Converters;

public class DateToStringConverter : IValueConverter
{
    private readonly ResourceLoader _resourceLoader = new();

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
                    ? GetResource("DateToStringOverdueOneDay", "Overdue by 1 day")
                    : string.Format(GetResource("DateToStringOverdueManyDays", "Overdue by {0} days"), daysOverdue);
            }
            else if (daysUntil == 0)
            {
                return GetResource("DateToStringDueToday", "Due Today");
            }
            else if (daysUntil == 1)
            {
                return GetResource("DateToStringDueTomorrow", "Due Tomorrow");
            }
            else if (daysUntil <= 7)
            {
                return string.Format(GetResource("DateToStringDueDayOfWeek", "Due {0}"), date.ToString("dddd"));
            }
            else
            {
                return string.Format(GetResource("DateToStringDueDate", "Due {0}"), date.ToString("MMM d"));
            }
        }

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private string GetResource(string key, string fallback)
    {
        var value = _resourceLoader.GetString(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
