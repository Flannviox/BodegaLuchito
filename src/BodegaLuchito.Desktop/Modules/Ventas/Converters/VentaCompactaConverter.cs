using System.Globalization;
using System.Windows.Data;

namespace BodegaLuchito.Desktop.Modules.Ventas.Converters;

public sealed class VentaCompactaConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is double ancho && ancho < 1000;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
