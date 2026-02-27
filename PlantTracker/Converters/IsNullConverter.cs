using System.Globalization;

namespace PlantTracker.Converters;

/// <summary>
/// Returns true when the value is null or empty string (i.e. the opposite of IsNotNullConverter).
/// Used to show a placeholder label when a nullable field has no value.
/// </summary>
public class IsNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is null || value.ToString() == string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}


