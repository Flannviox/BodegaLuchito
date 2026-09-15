using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;

namespace BodegaLuchito.Desktop.Modules.Productos.Views;

public partial class RegistrarProductoView : UserControl
{
    public RegistrarProductoView()
    {
        InitializeComponent();
    }

    // Evento puramente visual para bloquear letras y caracteres especiales
    private void ValidarSoloNumerosDecimales(object sender, TextCompositionEventArgs e)
    {
        // Solo permite números y el punto decimal
        Regex regex = new Regex("[^0-9.]+");
        e.Handled = regex.IsMatch(e.Text);
    }
}
