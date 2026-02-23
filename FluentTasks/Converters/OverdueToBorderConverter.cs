using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace FluentTasks.UI.Converters;

/// <summary>
/// Converts an IsOverdue bool to the appropriate border brush.
/// Uses WinUI3 SystemFillColorCriticalBrush for overdue, SystemFillColorNeutralBrush otherwise.
/// </summary>
public class OverdueToBorderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var resourceKey = value is bool isOverdue && isOverdue
            ? "SystemFillColorCriticalBrush"
            : "ControlStrokeColorDefaultBrush";

        return GetThemeResource(resourceKey) ?? new SolidColorBrush(Microsoft.UI.Colors.Gray);
    }

    private static Brush? GetThemeResource(string resourceKey)
    {
        if (Application.Current.Resources.TryGetValue(resourceKey, out var resource))
        {
            return resource as Brush;
        }
        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
