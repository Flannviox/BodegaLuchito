using BodegaLuchito.Application.Autenticacion.UseCases;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;

public partial class AutenticacionWindowViewModel : ViewModelBase
{
    private readonly RequiereConfiguracionInicialUseCase
        _requiereConfiguracionInicialUseCase;

    private readonly LoginViewModel _loginViewModel;

    private readonly ConfiguracionInicialViewModel
        _configuracionInicialViewModel;

    [ObservableProperty]
    private ViewModelBase? currentViewModel;

    public event Action? AutenticacionCompletada;

    public AutenticacionWindowViewModel(
        RequiereConfiguracionInicialUseCase
            requiereConfiguracionInicialUseCase,
        LoginViewModel loginViewModel,
        ConfiguracionInicialViewModel
            configuracionInicialViewModel)
    {
        _requiereConfiguracionInicialUseCase =
            requiereConfiguracionInicialUseCase;

        _loginViewModel = loginViewModel;

        _configuracionInicialViewModel =
            configuracionInicialViewModel;

        _loginViewModel.SesionIniciada +=
            OnSesionIniciada;

        _configuracionInicialViewModel.AdministradorCreado +=
            OnAdministradorCreado;
    }

    public async Task InicializarAsync(
        CancellationToken cancellationToken = default)
    {
        var requiereConfiguracion =
            await _requiereConfiguracionInicialUseCase
                .EjecutarAsync(cancellationToken);

        if (requiereConfiguracion)
        {
            CurrentViewModel =
                _configuracionInicialViewModel;

            return;
        }

        CurrentViewModel =
            _loginViewModel;
    }

    private void OnAdministradorCreado()
    {
        CurrentViewModel =
            _loginViewModel;
    }

    private void OnSesionIniciada()
    {
        AutenticacionCompletada?.Invoke();
    }
}
