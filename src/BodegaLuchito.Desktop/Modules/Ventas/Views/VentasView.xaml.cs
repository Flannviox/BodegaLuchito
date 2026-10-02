using System.Windows.Controls;
using System.Windows.Input;
using BodegaLuchito.Desktop.Modules.Ventas.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Ventas.Views;

public partial class VentasView : UserControl
{
    public VentasView()
    {
        InitializeComponent();
    }

  
    private void FilaProducto_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is VentasViewModel viewModel)
        {
            // Ejecutamos el comando de agregar al carrito manualmente
            if (viewModel.AgregarAlCarritoManualCommand.CanExecute(null))
            {
                viewModel.AgregarAlCarritoManualCommand.Execute(null);
            }
        }
    }
}
