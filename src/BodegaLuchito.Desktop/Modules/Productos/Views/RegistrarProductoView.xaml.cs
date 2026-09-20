using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Input;
using BodegaLuchito.Desktop.Modules.Productos.ViewModels;
using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Desktop.Modules.Productos.Views;

public partial class RegistrarProductoView : UserControl
{

    // Variables para medir la velocidad de escritura
    private DateTime _ultimaTeclaTiempo = DateTime.Now;
    private string _bufferLectora = string.Empty;

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

    // Filtro Anti-Humanos (Mide la velocidad)
    private void TxtCodigoBarras_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Bloqueamos absolutamente TODO ingreso nativo a la caja de texto
        e.Handled = true;

        TimeSpan tiempoTranscurrido = DateTime.Now - _ultimaTeclaTiempo;
        _ultimaTeclaTiempo = DateTime.Now;

        // Si pasaron más de 50 milisegundos, es un humano tipeando (o el primer dígito del escáner)
        if (tiempoTranscurrido.TotalMilliseconds > 50)
        {
            _bufferLectora = e.Text; // Reiniciamos el buffer
        }
        else
        {
            // Si entró rapidísimo (menos de 50ms), es la lectora disparando. Lo acumulamos.
            _bufferLectora += e.Text;
        }
    }

    // Bloquear teclas especiales (Retroceso y Espacio)
    private void TxtCodigoBarras_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Space || e.Key == Key.Back)
        {
            e.Handled = true; 
        }
    }

    //El Enter final que envía la lectora
    private void TxtCodigoBarras_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;

            // Un código de barras real suele tener al menos 4 números. 
            // Si el buffer tiene eso, significa que la lectora hizo su trabajo.
            if (_bufferLectora.Length >= 4)
            {
                // Inyectamos el texto artificialmente a la caja
                TxtCodigoBarras.Text = _bufferLectora;

                // Forzamos a que el ViewModel se entere del cambio (porque bloqueamos el tipeo normal)
                TxtCodigoBarras.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }

            _bufferLectora = string.Empty; // Limpiamos para el próximo uso

            TxtPrecioVenta.Focus();
            TxtPrecioVenta.SelectAll();
        }
    }
}
