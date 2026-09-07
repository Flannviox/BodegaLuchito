namespace BodegaLuchito.Application.Common.Session;

public interface ISesionUsuario
{
    UsuarioSesion? UsuarioActual { get; }

    bool EstaAutenticado { get; }

    event Action? SesionCambiada;

    void IniciarSesion(UsuarioSesion usuario);

    void CerrarSesion();
}
