using System.Windows.Controls;
using BodegaLuchito.Desktop.Modules.Caja.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Caja.Views;

public partial class CajaView : UserControl
{
    public CajaView()
    {
        InitializeComponent();

        Loaded += async (_, _) =>
        {
            if (DataContext is CajaViewModel viewModel)
            {
                await viewModel.InicializarAsync();
            }
        };
    }
}
