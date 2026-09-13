using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.UseCases;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;

public partial class ConfiguracionInicialViewModel : ViewModelBase
{
    private readonly CrearAdministradorInicialUseCase _crearAdministradorUseCase;

    [ObservableProperty]
    private string nombreCompleto = string.Empty;

    [ObservableProperty]
    private string nombreUsuario = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmarPassword = string.Empty;

    [ObservableProperty]
    private string mensajeError = string.Empty;

    [ObservableProperty]
    private bool estaProcesando;

    public event Action? AdministradorCreado;

    public ConfiguracionInicialViewModel(
        CrearAdministradorInicialUseCase crearAdministradorUseCase)
    {
        _crearAdministradorUseCase = crearAdministradorUseCase;
    }

    [RelayCommand]
    private async Task CrearAdministradorAsync()
    {
        if (EstaProcesando)
        {
            return;
        }

        try
        {
            EstaProcesando = true;
            MensajeError = string.Empty;

            var error =
                await _crearAdministradorUseCase.EjecutarAsync(
                    new CrearAdministradorInicialRequest
                    {
                        NombreCompleto = NombreCompleto,
                        NombreUsuario = NombreUsuario,
                        Password = Password,
                        ConfirmarPassword = ConfirmarPassword
                    });

            if (error is not null)
            {
                MensajeError = error;
                return;
            }

            Password = string.Empty;
            ConfirmarPassword = string.Empty;

            AdministradorCreado?.Invoke();
        }
        finally
        {
            EstaProcesando = false;
        }
    }
}
