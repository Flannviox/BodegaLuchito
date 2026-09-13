using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Application.Common.Session;

public sealed class UsuarioSesion
{
    public int IdUsuario { get; init; }

    public string NombreCompleto { get; init; } = string.Empty;

    public string NombreUsuario { get; init; } = string.Empty;

    public RolUsuario Rol { get; init; }

    public string NombreRol => Rol.ToString();

    public bool EsAdministradora =>
        Rol == RolUsuario.Administradora;
}
