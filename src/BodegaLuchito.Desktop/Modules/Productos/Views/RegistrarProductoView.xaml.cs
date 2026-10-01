using System.Windows.Controls;
using System.Windows.Input;

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
