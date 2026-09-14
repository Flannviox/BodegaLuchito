using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Application.Autenticacion.DTOs;

public sealed class RegistrarUsuarioRequest
{
    public string NombreCompleto { get; init; } = string.Empty;

    public string NombreUsuario { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string ConfirmarPassword { get; init; } = string.Empty;

    public RolUsuario Rol { get; init; }
}
