using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Application.Autenticacion.DTOs;

public sealed class UsuarioDto
{
    public int Id { get; init; }

    public string NombreCompleto { get; init; } = string.Empty;

    public string NombreUsuario { get; init; } = string.Empty;

    public RolUsuario Rol { get; init; }

    public string NombreRol => Rol.ToString();

    public bool Activo { get; init; }

    public string EstadoTexto =>
        Activo ? "Activo" : "Inactivo";

    public DateTime FechaCreacion { get; init; }
}
