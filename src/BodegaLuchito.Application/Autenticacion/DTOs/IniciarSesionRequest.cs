namespace BodegaLuchito.Application.Autenticacion.DTOs;

public sealed class IniciarSesionRequest
{
    public string NombreUsuario { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}
