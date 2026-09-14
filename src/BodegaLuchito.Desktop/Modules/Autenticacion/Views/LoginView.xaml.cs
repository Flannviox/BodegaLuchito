using System.Windows;
using System.Windows.Controls;
using BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Autenticacion.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private void PasswordInput_OnPasswordChanged(
        object sender,
        RoutedEventArgs e)
    {
        if (DataContext is LoginViewModel viewModel
            && sender is PasswordBox passwordBox)
        {
            viewModel.Password = passwordBox.Password;
        }
    }
}
