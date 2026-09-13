using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Application.Autenticacion.UseCases;

public sealed class RegistrarUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISesionUsuario _sesionUsuario;

    public RegistrarUsuarioUseCase(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        ISesionUsuario sesionUsuario)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _sesionUsuario = sesionUsuario;
    }

    public async Task<string?> EjecutarAsync(
        RegistrarUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora != true)
        {
            return "No tiene permisos para registrar usuarios.";
        }

        var nombreCompleto = request.NombreCompleto.Trim();
        var nombreUsuario = request.NombreUsuario.Trim();

        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            return "Ingrese el nombre completo.";
        }

        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            return "Ingrese el nombre de usuario.";
        }

        if (!Enum.IsDefined(request.Rol))
        {
            return "Seleccione un rol válido.";
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return "Ingrese una contraseña.";
        }

        if (request.Password.Length < 8)
        {
            return "La contraseña debe contener al menos 8 caracteres.";
        }

        if (request.Password != request.ConfirmarPassword)
        {
            return "Las contraseñas no coinciden.";
        }

        if (await _usuarioRepository.ExisteNombreUsuarioAsync(
                nombreUsuario,
                cancellationToken: cancellationToken))
        {
            return "El nombre de usuario ya está registrado.";
        }

        var usuario = new Usuario
        {
            NombreCompleto = nombreCompleto,
            NombreUsuario = nombreUsuario,
            PasswordHash =
                _passwordHasher.GenerarHash(request.Password),
            Rol = request.Rol,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        await _usuarioRepository.AgregarAsync(
            usuario,
            cancellationToken);

        return null;
    }
}
