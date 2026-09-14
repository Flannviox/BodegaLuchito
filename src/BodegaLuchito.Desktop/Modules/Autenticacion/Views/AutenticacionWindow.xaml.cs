using System.Windows;
using BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Autenticacion.Views;

public partial class AutenticacionWindow : Window
{
    private readonly AutenticacionWindowViewModel _viewModel;

    public AutenticacionWindow(
        AutenticacionWindowViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public Task InicializarAsync()
    {
        return _viewModel.InicializarAsync();
    }
}
