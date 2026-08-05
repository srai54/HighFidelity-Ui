using System.Globalization;

namespace HighFidelity.Ui.Converters;

/// <summary>Turns a count into a visibility bool — used to hide a warning element entirely when there's nothing to warn about.</summary>
public class IsGreaterThanZeroConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is int count && count > 0;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
