using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Application.Common.Session;

namespace BodegaLuchito.Application.Autenticacion.UseCases;

public sealed class RestablecerPasswordUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISesionUsuario _sesionUsuario;

    public RestablecerPasswordUsuarioUseCase(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        ISesionUsuario sesionUsuario)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _sesionUsuario = sesionUsuario;
    }

    public async Task<string?> EjecutarAsync(
        RestablecerPasswordUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora != true)
        {
            return "No tiene permisos para modificar contraseñas.";
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return "Ingrese la nueva contraseña.";
        }

        if (request.Password.Length < 8)
        {
            return "La contraseña debe contener al menos 8 caracteres.";
        }

        if (request.Password != request.ConfirmarPassword)
        {
            return "Las contraseñas no coinciden.";
        }

        var usuario =
            await _usuarioRepository.ObtenerPorIdAsync(
                request.UsuarioId,
                cancellationToken);

        if (usuario is null)
        {
            return "El usuario no existe.";
        }

        usuario.PasswordHash =
            _passwordHasher.GenerarHash(
                request.Password);

        usuario.FechaActualizacion =
            DateTime.Now;

        await _usuarioRepository.ActualizarAsync(
            usuario,
            cancellationToken);

        return null;
    }
}
