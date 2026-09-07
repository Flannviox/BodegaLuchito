namespace BodegaLuchito.Application.Common.Session;

public sealed class SesionUsuario : ISesionUsuario
{
    public UsuarioSesion? UsuarioActual { get; private set; }

    public bool EstaAutenticado =>
        UsuarioActual is not null;

    public event Action? SesionCambiada;

    public void IniciarSesion(UsuarioSesion usuario)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        UsuarioActual = usuario;

        SesionCambiada?.Invoke();
    }

    public void CerrarSesion()
    {
        UsuarioActual = null;

        SesionCambiada?.Invoke();
    }
}
