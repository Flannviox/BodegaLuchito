using System.Windows;
using System.Windows.Controls;
using BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Autenticacion.Views;

public partial class ConfiguracionInicialView : UserControl
{
    public ConfiguracionInicialView()
    {
        InitializeComponent();
    }

    private void PasswordInput_OnPasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is ConfiguracionInicialViewModel viewModel
            && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }

    private void ConfirmPasswordInput_OnPasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is ConfiguracionInicialViewModel viewModel
            && sender is PasswordBox passwordBox)
        {
            viewModel.ConfirmarPassword = passwordBox.Password;
        }
    }
}
