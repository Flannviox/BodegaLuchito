using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Autenticacion.Views;

public partial class LoginView : UserControl
{
    private LoginViewModel? _viewModel;
    private bool _sincronizandoPassword;

    public LoginView()
    {
        InitializeComponent();
    }

    private void LoginView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null) _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
        _viewModel = DataContext as LoginViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
            SincronizarPassword(_viewModel.Password);
        }
        UsuarioInput.Focus();
    }

    private void LoginView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
            _viewModel.Password = string.Empty;
            _viewModel = null;
        }
        MostrarPasswordButton.IsChecked = false;
        SincronizarPassword(string.Empty);
    }

    private void ViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LoginViewModel.Password) && _viewModel is not null)
        {
            SincronizarPassword(_viewModel.Password);
            if (string.IsNullOrEmpty(_viewModel.Password)) MostrarPasswordButton.IsChecked = false;
        }
    }

    // Solo sincroniza los controles visuales; la autenticación permanece en el ViewModel.
    private void SincronizarPassword(string password)
    {
        _sincronizandoPassword = true;
        try
        {
            if (PasswordInput.Password != password) PasswordInput.Password = password;
            var textoVisible = MostrarPasswordButton.IsChecked == true ? password : string.Empty;
            if (PasswordVisibleInput.Text != textoVisible) PasswordVisibleInput.Text = textoVisible;
        }
        finally { _sincronizandoPassword = false; }
    }

    private void PasswordInput_OnPasswordChanged(object sender, RoutedEventArgs e)
    {
        if (!_sincronizandoPassword && DataContext is LoginViewModel viewModel)
            viewModel.Password = PasswordInput.Password;
    }

    private void PasswordVisibleInput_OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_sincronizandoPassword && MostrarPasswordButton.IsChecked == true
            && DataContext is LoginViewModel viewModel)
            viewModel.Password = PasswordVisibleInput.Text;
    }

    private void MostrarPassword_OnChanged(object sender, RoutedEventArgs e)
    {
        var mostrar = MostrarPasswordButton.IsChecked == true;
        SincronizarPassword(PasswordInput.Password);
        PasswordInput.Visibility = mostrar ? Visibility.Collapsed : Visibility.Visible;
        PasswordVisibleInput.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
        if (mostrar)
        {
            PasswordVisibleInput.Focus();
            PasswordVisibleInput.CaretIndex = PasswordVisibleInput.Text.Length;
        }
        else PasswordInput.Focus();
    }

    private void IniciarSesion_OnClick(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(UsuarioInput.Text)) UsuarioInput.Focus();
    }
}