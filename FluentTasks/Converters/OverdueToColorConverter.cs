using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace FluentTasks.UI.Converters;

/// <summary>
/// Converts an IsOverdue bool to the appropriate foreground brush.
/// Uses WinUI3 SystemFillColorCriticalBrush for overdue, TextFillColorPrimaryBrush otherwise.
/// </summary>
public class OverdueToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var resourceKey = value is bool isOverdue && isOverdue
            ? "SystemFillColorCriticalBrush"
            : "TextFillColorPrimaryBrush";

        return GetThemeResource(resourceKey) ?? new SolidColorBrush(Microsoft.UI.Colors.Black);
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
