using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using BodegaLuchito.Desktop.Modules.Productos.ViewModels;
using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Desktop.Modules.Productos.Views;

public partial class RegistrarProductoView : UserControl
{
    public RegistrarProductoView()
    {
        InitializeComponent();
    }

    // Regla para el Precio (Siempre permite decimales)
    private void ValidarSoloNumerosDecimales(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("[^0-9.]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    // Regla inteligente para el Stock (Depende del ComboBox)
    private void ValidarEntradaStock(object sender, TextCompositionEventArgs e)
    {
        if (DataContext is RegistrarProductoViewModel viewModel)
        {
            if (viewModel.UnidadVenta == UnidadVenta.Unidad)
            {
                // Solo permite números enteros (Bloquea el punto)
                Regex regexEntero = new Regex("[^0-9]+");
                e.Handled = regexEntero.IsMatch(e.Text);
                return;
            }
        }

        // Si es por Peso, permite el punto decimal
        Regex regexDecimal = new Regex("[^0-9.]+");
        e.Handled = regexDecimal.IsMatch(e.Text);
    }
}
