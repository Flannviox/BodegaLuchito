using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.UseCases;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IniciarSesionUseCase _iniciarSesionUseCase;

    [ObservableProperty]
    private string nombreUsuario = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string mensajeError = string.Empty;

    [ObservableProperty]
    private bool estaProcesando;

    public LoginViewModel(
        IniciarSesionUseCase iniciarSesionUseCase)
    {
        _iniciarSesionUseCase = iniciarSesionUseCase;
    }

    [RelayCommand]
    private async Task IniciarSesionAsync()
    {
        if (EstaProcesando)
        {
            return;
        }

        try
        {
            EstaProcesando = true;
            MensajeError = string.Empty;

            var resultado =
                await _iniciarSesionUseCase.EjecutarAsync(
                    new IniciarSesionRequest
                    {
                        NombreUsuario = NombreUsuario,
                        Password = Password
                    });

            if (!resultado.Exitoso)
            {
                MensajeError = resultado.Mensaje;
                return;
            }

            Password = string.Empty;

            SesionIniciada?.Invoke();
        }
        finally
        {
            EstaProcesando = false;
        }
    }
    public event Action? SesionIniciada;
}
