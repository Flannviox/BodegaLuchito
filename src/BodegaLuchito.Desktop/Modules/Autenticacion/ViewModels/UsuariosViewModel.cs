using System.Collections.ObjectModel;
using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.UseCases;
using BodegaLuchito.Desktop.Common.ViewModels;
using BodegaLuchito.Domain.Autenticacion.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;

public partial class UsuariosViewModel : ViewModelBase
{
    private readonly RegistrarUsuarioUseCase _registrarUsuarioUseCase;
    private readonly ListarUsuariosUseCase _listarUsuariosUseCase;
    private readonly ModificarUsuarioUseCase _modificarUsuarioUseCase;
    private readonly CambiarEstadoUsuarioUseCase _cambiarEstadoUsuarioUseCase;
    private readonly RestablecerPasswordUsuarioUseCase
        _restablecerPasswordUsuarioUseCase;

    private List<UsuarioDto> _todosUsuarios = [];

    public UsuariosViewModel(
        RegistrarUsuarioUseCase registrarUsuarioUseCase,
        ListarUsuariosUseCase listarUsuariosUseCase,
        ModificarUsuarioUseCase modificarUsuarioUseCase,
        CambiarEstadoUsuarioUseCase cambiarEstadoUsuarioUseCase,
        RestablecerPasswordUsuarioUseCase restablecerPasswordUsuarioUseCase)
    {
        _registrarUsuarioUseCase = registrarUsuarioUseCase;
        _listarUsuariosUseCase = listarUsuariosUseCase;
        _modificarUsuarioUseCase = modificarUsuarioUseCase;
        _cambiarEstadoUsuarioUseCase = cambiarEstadoUsuarioUseCase;
        _restablecerPasswordUsuarioUseCase =
            restablecerPasswordUsuarioUseCase;
    }

    public ObservableCollection<UsuarioDto> Usuarios { get; } = [];

    public IReadOnlyList<RolUsuario> RolesDisponibles { get; } =
        Enum.GetValues<RolUsuario>();

    [ObservableProperty]
    private string textoBusqueda = string.Empty;

    [ObservableProperty]
    private UsuarioDto? usuarioSeleccionado;

    [ObservableProperty]
    private bool mostrarFormulario;

    [ObservableProperty]
    private bool esNuevoUsuario;

    [ObservableProperty]
    private string nombreCompletoEdicion = string.Empty;

    [ObservableProperty]
    private string nombreUsuarioEdicion = string.Empty;

    [ObservableProperty]
    private RolUsuario rolSeleccionado = RolUsuario.Vendedora;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private string confirmarPassword = string.Empty;

    [ObservableProperty]
    private bool mostrarRestablecerPassword;

    [ObservableProperty]
    private string nuevaPassword = string.Empty;

    [ObservableProperty]
    private string confirmarNuevaPassword = string.Empty;

    [ObservableProperty]
    private string mensajeError = string.Empty;

    [ObservableProperty]
    private string mensajeExito = string.Empty;

    [ObservableProperty]
    private bool mostrarMensajeExito;

    [ObservableProperty]
    private bool estaProcesando;

    public string TituloFormulario =>
        EsNuevoUsuario
            ? "Nuevo usuario"
            : "Editar usuario";

    public string TextoCambiarEstado =>
        UsuarioSeleccionado?.Activo == true
            ? "Desactivar"
            : "Activar";

    public async Task InicializarAsync()
    {
        await CargarUsuariosAsync();
    }

    private async Task CargarUsuariosAsync()
    {
        try
        {
            EstaProcesando = true;
            MensajeError = string.Empty;

            var usuarios =
                await _listarUsuariosUseCase.EjecutarAsync();

            _todosUsuarios =
                usuarios.ToList();

            AplicarFiltro();
        }
        catch (UnauthorizedAccessException ex)
        {
            MensajeError = ex.Message;
        }
        finally
        {
            EstaProcesando = false;
        }
    }

    partial void OnTextoBusquedaChanged(
        string value)
    {
        AplicarFiltro();
    }

    partial void OnUsuarioSeleccionadoChanged(
        UsuarioDto? value)
    {
        OnPropertyChanged(
            nameof(TextoCambiarEstado));

        EditarUsuarioCommand.NotifyCanExecuteChanged();
        CambiarEstadoUsuarioCommand.NotifyCanExecuteChanged();
        AbrirRestablecerPasswordCommand.NotifyCanExecuteChanged();
    }

    partial void OnEsNuevoUsuarioChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(TituloFormulario));
    }

    private void AplicarFiltro()
    {
        var texto =
            TextoBusqueda.Trim();

        IEnumerable<UsuarioDto> resultado =
            _todosUsuarios;

        if (!string.IsNullOrWhiteSpace(texto))
        {
            resultado =
                resultado.Where(
                    x =>
                        x.NombreCompleto.Contains(
                            texto,
                            StringComparison.OrdinalIgnoreCase)
                        || x.NombreUsuario.Contains(
                            texto,
                            StringComparison.OrdinalIgnoreCase)
                        || x.NombreRol.Contains(
                            texto,
                            StringComparison.OrdinalIgnoreCase));
        }

        Usuarios.Clear();

        foreach (var usuario in resultado)
        {
            Usuarios.Add(usuario);
        }
    }

    [RelayCommand]
    private void NuevoUsuario()
    {
        LimpiarMensajes();

        EsNuevoUsuario = true;

        NombreCompletoEdicion = string.Empty;
        NombreUsuarioEdicion = string.Empty;
        RolSeleccionado = RolUsuario.Vendedora;
        Password = string.Empty;
        ConfirmarPassword = string.Empty;

        MostrarRestablecerPassword = false;
        MostrarFormulario = true;
    }

    private bool PuedeEditarUsuario()
    {
        return UsuarioSeleccionado is not null;
    }

    [RelayCommand(CanExecute = nameof(PuedeEditarUsuario))]
    private void EditarUsuario()
    {
        if (UsuarioSeleccionado is null)
        {
            return;
        }

        LimpiarMensajes();

        EsNuevoUsuario = false;

        NombreCompletoEdicion =
            UsuarioSeleccionado.NombreCompleto;

        NombreUsuarioEdicion =
            UsuarioSeleccionado.NombreUsuario;

        RolSeleccionado =
            UsuarioSeleccionado.Rol;

        Password = string.Empty;
        ConfirmarPassword = string.Empty;

        MostrarRestablecerPassword = false;
        MostrarFormulario = true;
    }

    [RelayCommand]
    private async Task GuardarUsuarioAsync()
    {
        LimpiarMensajes();

        string? error;

        if (EsNuevoUsuario)
        {
            error =
                await _registrarUsuarioUseCase.EjecutarAsync(
                    new RegistrarUsuarioRequest
                    {
                        NombreCompleto =
                            NombreCompletoEdicion,

                        NombreUsuario =
                            NombreUsuarioEdicion,

                        Password =
                            Password,

                        ConfirmarPassword =
                            ConfirmarPassword,

                        Rol =
                            RolSeleccionado
                    });
        }
        else
        {
            if (UsuarioSeleccionado is null)
            {
                return;
            }

            error =
                await _modificarUsuarioUseCase.EjecutarAsync(
                    new ModificarUsuarioRequest
                    {
                        Id =
                            UsuarioSeleccionado.Id,

                        NombreCompleto =
                            NombreCompletoEdicion,

                        NombreUsuario =
                            NombreUsuarioEdicion,

                        Rol =
                            RolSeleccionado
                    });
        }

        if (error is not null)
        {
            MensajeError = error;
            return;
        }

        MostrarFormulario = false;

        _ = MostrarExitoTemporalAsync(
            EsNuevoUsuario
                ? "Usuario registrado correctamente."
                : "Usuario actualizado correctamente.");

        await CargarUsuariosAsync();
    }

    [RelayCommand]
    private void CancelarFormulario()
    {
        MostrarFormulario = false;

        Password = string.Empty;
        ConfirmarPassword = string.Empty;

        LimpiarMensajes();
    }

    private bool PuedeCambiarEstadoUsuario()
    {
        return UsuarioSeleccionado is not null;
    }

    [RelayCommand(
        CanExecute = nameof(PuedeCambiarEstadoUsuario))]
    private async Task CambiarEstadoUsuarioAsync()
    {
        if (UsuarioSeleccionado is null)
        {
            return;
        }

        LimpiarMensajes();

        var activar =
            !UsuarioSeleccionado.Activo;

        var error =
            await _cambiarEstadoUsuarioUseCase.EjecutarAsync(
                UsuarioSeleccionado.Id,
                activar);

        if (error is not null)
        {
            MensajeError = error;
            return;
        }

        _ = MostrarExitoTemporalAsync(
            activar
                ? "Usuario activado correctamente."
                : "Usuario desactivado correctamente.");

        UsuarioSeleccionado = null;

        await CargarUsuariosAsync();
    }

    private bool PuedeAbrirRestablecerPassword()
    {
        return UsuarioSeleccionado is not null;
    }

    [RelayCommand(
        CanExecute = nameof(PuedeAbrirRestablecerPassword))]
    private void AbrirRestablecerPassword()
    {
        if (UsuarioSeleccionado is null)
        {
            return;
        }

        LimpiarMensajes();

        NuevaPassword = string.Empty;
        ConfirmarNuevaPassword = string.Empty;

        MostrarFormulario = false;
        MostrarRestablecerPassword = true;
    }

    [RelayCommand]
    private async Task GuardarNuevaPasswordAsync()
    {
        if (UsuarioSeleccionado is null)
        {
            return;
        }

        LimpiarMensajes();

        var error =
            await _restablecerPasswordUsuarioUseCase.EjecutarAsync(
                new RestablecerPasswordUsuarioRequest
                {
                    UsuarioId =
                        UsuarioSeleccionado.Id,

                    Password =
                        NuevaPassword,

                    ConfirmarPassword =
                        ConfirmarNuevaPassword
                });

        if (error is not null)
        {
            MensajeError = error;
            return;
        }

        MostrarRestablecerPassword = false;

        NuevaPassword = string.Empty;
        ConfirmarNuevaPassword = string.Empty;

        _ = MostrarExitoTemporalAsync(
                "Contraseña actualizada correctamente.");
    }

    [RelayCommand]
    private void CancelarRestablecerPassword()
    {
        MostrarRestablecerPassword = false;

        NuevaPassword = string.Empty;
        ConfirmarNuevaPassword = string.Empty;

        LimpiarMensajes();
    }

    private void LimpiarMensajes()
    {
        MensajeError = string.Empty;
    }
    private async Task MostrarExitoTemporalAsync(
    string mensaje)
    {
        MensajeExito = mensaje;
        MostrarMensajeExito = true;

        await Task.Delay(2500);

        MostrarMensajeExito = false;
        MensajeExito = string.Empty;
    }
}
