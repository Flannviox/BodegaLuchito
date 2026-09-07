namespace BodegaLuchito.Application.Common.Session;

public sealed class UsuarioSesion
{
    public int IdUsuario { get; init; }

    public string NombreUsuario { get; init; } = string.Empty;

    public string NombreRol { get; init; } = string.Empty;
}
