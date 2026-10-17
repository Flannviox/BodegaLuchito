using System.Windows;
using System.Windows.Controls;
using BodegaLuchito.Desktop.Modules.BI.ViewModels;

namespace BodegaLuchito.Desktop.Modules.BI.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();

        // Cuando la vista termine de dibujarse en pantalla, cargará los datos
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is DashboardViewModel viewModel)
        {
            await viewModel.InicializarAsync();
        }
    }
}
