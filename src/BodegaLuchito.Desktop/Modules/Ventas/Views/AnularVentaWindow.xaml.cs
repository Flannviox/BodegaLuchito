using System.Windows;

namespace BodegaLuchito.Desktop.Modules.Ventas.Views;

public partial class AnularVentaWindow : Window
{
    public string MotivoAnulacion { get; private set; } = string.Empty;

    public AnularVentaWindow()
    {
        InitializeComponent();
        txtMotivo.Focus();
    }

    private void BtnConfirmar_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtMotivo.Text))
        {
            MessageBox.Show("Debe ingresar un motivo para anular la venta.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        MotivoAnulacion = txtMotivo.Text.Trim();
        DialogResult = true;
        Close();
    }

    private void BtnCancelar_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
