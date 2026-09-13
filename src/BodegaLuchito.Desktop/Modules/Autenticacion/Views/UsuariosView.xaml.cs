using System.Windows;
using System.Windows.Controls;
using BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Autenticacion.Views;

public partial class UsuariosView : UserControl
{
    private bool _inicializado;

    public UsuariosView()
    {
        InitializeComponent();
    }

    private async void UsuariosView_OnLoaded(
        object sender,
        RoutedEventArgs e)
    {
        if (_inicializado)
        {
            return;
        }

        if (DataContext is UsuariosViewModel viewModel)
        {
            _inicializado = true;

            await viewModel.InicializarAsync();
        }
    }

    private void PasswordNuevo_OnPasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is UsuariosViewModel viewModel
            && sender is PasswordBox passwordBox)
        {
            viewModel.Password =
                passwordBox.Password;
        }
    }

    private void ConfirmarPasswordNuevo_OnPasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is UsuariosViewModel viewModel
            && sender is PasswordBox passwordBox)
        {
            viewModel.ConfirmarPassword =
                passwordBox.Password;
        }
    }

    private void NuevaPasswordInput_OnPasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is UsuariosViewModel viewModel
            && sender is PasswordBox passwordBox)
        {
            viewModel.NuevaPassword =
                passwordBox.Password;
        }
    }

    private void ConfirmarNuevaPasswordInput_OnPasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is UsuariosViewModel viewModel
            && sender is PasswordBox passwordBox)
        {
            viewModel.ConfirmarNuevaPassword =
                passwordBox.Password;
        }
    }
}
