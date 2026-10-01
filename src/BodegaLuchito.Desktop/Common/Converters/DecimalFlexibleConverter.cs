using System;
using System.Globalization;
using System.Windows.Data;

namespace BodegaLuchito.Desktop.Common.Converters;

public sealed class DecimalFlexibleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var decimalPlaces = GetDecimalPlaces(parameter);
        var format = decimalPlaces == 0
            ? "0"
            : $"0.{new string('#', decimalPlaces)}";

        return value is decimal decimalValue
            ? decimalValue.ToString(format, culture)
            : string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var text = value?.ToString()?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(text) ||
            text.EndsWith(".", StringComparison.Ordinal) ||
            text.EndsWith(",", StringComparison.Ordinal))
        {
            return Binding.DoNothing;
        }

        var normalized = text.Replace(',', '.');
        if (!decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out var decimalValue))
        {
            return Binding.DoNothing;
        }

        return Math.Round(
            decimalValue,
            GetDecimalPlaces(parameter),
            MidpointRounding.AwayFromZero);
    }

    private static int GetDecimalPlaces(object parameter)
    {
        return int.TryParse(parameter?.ToString(), out var decimalPlaces)
            ? Math.Clamp(decimalPlaces, 0, 4)
            : 4;
    }
}
