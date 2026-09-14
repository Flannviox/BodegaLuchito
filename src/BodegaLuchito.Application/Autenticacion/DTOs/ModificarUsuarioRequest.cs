using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Application.Autenticacion.DTOs;

public sealed class ModificarUsuarioRequest
{
    public int Id { get; init; }

    public string NombreCompleto { get; init; } = string.Empty;

    public string NombreUsuario { get; init; } = string.Empty;

    public RolUsuario Rol { get; init; }
}
