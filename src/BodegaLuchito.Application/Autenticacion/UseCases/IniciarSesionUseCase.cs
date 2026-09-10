using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Application.Common.Session;

namespace BodegaLuchito.Application.Autenticacion.UseCases;

public sealed class IniciarSesionUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISesionUsuario _sesionUsuario;

    public IniciarSesionUseCase(
        IUsuarioRepository usuarioRepository,
        IPasswordHasher passwordHasher,
        ISesionUsuario sesionUsuario)
    {
        _usuarioRepository = usuarioRepository;
        _passwordHasher = passwordHasher;
        _sesionUsuario = sesionUsuario;
    }

    public async Task<IniciarSesionResult> EjecutarAsync(
        IniciarSesionRequest request,
        CancellationToken cancellationToken = default)
    {
        var nombreUsuario =
            request.NombreUsuario.Trim();

        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            return IniciarSesionResult.Fallido(
                "Ingrese el nombre de usuario.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return IniciarSesionResult.Fallido(
                "Ingrese la contraseña.");
        }

        var usuario =
            await _usuarioRepository.ObtenerPorNombreUsuarioAsync(
                nombreUsuario,
                cancellationToken);

        if (usuario is null)
        {
            return IniciarSesionResult.Fallido(
                "Usuario o contraseña incorrectos.");
        }

        if (!usuario.Activo)
        {
            return IniciarSesionResult.Fallido(
                "Usuario o contraseña incorrectos.");
        }

        var passwordCorrecto =
            _passwordHasher.Verificar(
                request.Password,
                usuario.PasswordHash);

        if (!passwordCorrecto)
        {
            return IniciarSesionResult.Fallido(
                "Usuario o contraseña incorrectos.");
        }

        _sesionUsuario.IniciarSesion(
            new UsuarioSesion
            {
                IdUsuario = usuario.Id,
                NombreUsuario = usuario.NombreUsuario,
                NombreRol = usuario.Rol.ToString()
            });

        return IniciarSesionResult.Correcto();
    }
}
