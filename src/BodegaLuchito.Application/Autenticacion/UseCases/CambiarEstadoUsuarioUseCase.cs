using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Application.Autenticacion.UseCases;

public sealed class CambiarEstadoUsuarioUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ISesionUsuario _sesionUsuario;

    public CambiarEstadoUsuarioUseCase(
        IUsuarioRepository usuarioRepository,
        ISesionUsuario sesionUsuario)
    {
        _usuarioRepository = usuarioRepository;
        _sesionUsuario = sesionUsuario;
    }

    public async Task<string?> EjecutarAsync(
        int usuarioId,
        bool activar,
        CancellationToken cancellationToken = default)
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora != true)
        {
            return "No tiene permisos para modificar usuarios.";
        }

        var usuario =
            await _usuarioRepository.ObtenerPorIdAsync(
                usuarioId,
                cancellationToken);

        if (usuario is null)
        {
            return "El usuario no existe.";
        }

        if (usuario.Activo == activar)
        {
            return null;
        }

        if (!activar
            && usuario.Id ==
            _sesionUsuario.UsuarioActual.IdUsuario)
        {
            return "No puede desactivar su propia cuenta.";
        }

        if (!activar
            && usuario.Rol == RolUsuario.Administradora)
        {
            var administradorasActivas =
                await _usuarioRepository
                    .ContarAdministradorasActivasAsync(
                        cancellationToken);

            if (administradorasActivas <= 1)
            {
                return "Debe existir al menos una administradora activa.";
            }
        }

        usuario.Activo = activar;
        usuario.FechaActualizacion = DateTime.Now;

        await _usuarioRepository.ActualizarAsync(
            usuario,
            cancellationToken);

        return null;
    }
}
