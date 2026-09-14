namespace BodegaLuchito.Application.Autenticacion.DTOs;

public sealed class RestablecerPasswordUsuarioRequest
{
    public int UsuarioId { get; init; }

    public string Password { get; init; } = string.Empty;

    public string ConfirmarPassword { get; init; } = string.Empty;
}
