using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Application.Autenticacion.UseCases;

public sealed class CrearAdministradorInicialUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;

    public CrearAdministradorInicialUseCase(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<string?> EjecutarAsync(
        CrearAdministradorInicialRequest request,
        CancellationToken cancellationToken = default)
    {
        if (await _usuarioRepository.ExisteAlgunUsuarioAsync(
                cancellationToken))
        {
            return "El sistema ya cuenta con usuarios registrados.";
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

        var usuario = new Usuario
        {
            NombreCompleto = nombreCompleto,
            NombreUsuario = nombreUsuario,
            PasswordHash =
                _passwordHasher.GenerarHash(request.Password),
            Rol = RolUsuario.Administradora,
            Activo = true
        };

        await _usuarioRepository.AgregarAsync(
            usuario,
            cancellationToken);

        return null;
    }
}
