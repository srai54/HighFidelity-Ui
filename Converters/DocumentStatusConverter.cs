using System.Globalization;

namespace HighFidelity.Ui.Converters;

/// <summary>
/// Maps ProcessingStatus string to a color for the status badge.
/// </summary>
public class DocumentStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Pending" => Color.FromArgb("#FF9800"),
            "Processing" => Color.FromArgb("#2196F3"),
            "Completed" => Color.FromArgb("#4CAF50"),
            "Failed" => Color.FromArgb("#F44336"),
            _ => Colors.Grey
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Maps ProcessingStatus string to a readable label.
/// </summary>
public class DocumentStatusLabelConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "Pending" => "Waiting",
            "Processing" => "Processing",
            "Completed" => "Completed",
            "Failed" => "Failed",
            _ => "Unknown"
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
