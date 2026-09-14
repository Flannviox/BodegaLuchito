using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Domain.Autenticacion.Entities;

public sealed class Usuario
{
    public int Id { get; set; }

    public string NombreCompleto { get; set; } = string.Empty;

    public string NombreUsuario { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public RolUsuario Rol { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;

    public DateTime? FechaActualizacion { get; set; }
}
