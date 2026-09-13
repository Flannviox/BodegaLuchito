using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Application.Autenticacion.UseCases;

public sealed class ModificarUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ISesionUsuario _sesionUsuario;

    public ModificarUsuarioUseCase(
        IUsuarioRepository usuarioRepository,
        ISesionUsuario sesionUsuario)
    {
        _usuarioRepository = usuarioRepository;
        _sesionUsuario = sesionUsuario;
    }

    public async Task<string?> EjecutarAsync(
        ModificarUsuarioRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora != true)
        {
            return "No tiene permisos para modificar usuarios.";
        }

        var usuario =
            await _usuarioRepository.ObtenerPorIdAsync(
                request.Id,
                cancellationToken);

        if (usuario is null)
        {
            return "El usuario no existe.";
        }

        var nombreCompleto =
            request.NombreCompleto.Trim();

        var nombreUsuario =
            request.NombreUsuario.Trim();

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

        if (await _usuarioRepository.ExisteNombreUsuarioAsync(
                nombreUsuario,
                usuario.Id,
                cancellationToken))
        {
            return "El nombre de usuario ya está registrado.";
        }

        var esUsuarioActual =
            usuario.Id ==
            _sesionUsuario.UsuarioActual!.IdUsuario;

        if (esUsuarioActual
            && usuario.Rol != request.Rol)
        {
            return "No puede modificar su propio rol.";
        }

        if (usuario.Rol == RolUsuario.Administradora
            && request.Rol != RolUsuario.Administradora
            && usuario.Activo)
        {
            var cantidadAdministradoras =
                await _usuarioRepository
                    .ContarAdministradorasActivasAsync(
                        cancellationToken);

            if (cantidadAdministradoras <= 1)
            {
                return "Debe existir al menos una administradora activa.";
            }
        }

        usuario.NombreCompleto = nombreCompleto;
        usuario.NombreUsuario = nombreUsuario;
        usuario.Rol = request.Rol;
        usuario.FechaActualizacion = DateTime.Now;

        await _usuarioRepository.ActualizarAsync(
            usuario,
            cancellationToken);

        if (esUsuarioActual)
        {
            _sesionUsuario.IniciarSesion(
                new UsuarioSesion
                {
                    IdUsuario = usuario.Id,
                    NombreCompleto = usuario.NombreCompleto,
                    NombreUsuario = usuario.NombreUsuario,
                    Rol = usuario.Rol
                });
        }
        return null;
    }
}
