using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Application.Common.Session;

namespace BodegaLuchito.Application.Autenticacion.UseCases;

public sealed class ListarUsuariosUseCase
{
    private readonly IUsuarioRepository _usuarioRepository;
    private readonly ISesionUsuario _sesionUsuario;

    public ListarUsuariosUseCase(
        IUsuarioRepository usuarioRepository,
        ISesionUsuario sesionUsuario)
    {
        _usuarioRepository = usuarioRepository;
        _sesionUsuario = sesionUsuario;
    }

    public async Task<IReadOnlyList<UsuarioDto>> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora != true)
        {
            throw new UnauthorizedAccessException(
                "No tiene permisos para consultar usuarios.");
        }

        var usuarios =
            await _usuarioRepository.ObtenerTodosAsync(
                cancellationToken);

        return usuarios
            .Select(
                x => new UsuarioDto
                {
                    Id = x.Id,
                    NombreCompleto = x.NombreCompleto,
                    NombreUsuario = x.NombreUsuario,
                    Rol = x.Rol,
                    Activo = x.Activo,
                    FechaCreacion = x.FechaCreacion
                })
            .ToList();
    }
}
