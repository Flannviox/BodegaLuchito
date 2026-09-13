namespace BodegaLuchito.Application.Autenticacion.DTOs;

public sealed class IniciarSesionResult
{
    private IniciarSesionResult(
        bool exitoso,
        string mensaje)
    {
        Exitoso = exitoso;
        Mensaje = mensaje;
    }

    public bool Exitoso { get; }

    public string Mensaje { get; }

    public static IniciarSesionResult Correcto()
    {
        return new IniciarSesionResult(
            true,
            "Inicio de sesion correcto.");
    }

    public static IniciarSesionResult Fallido(
        string mensaje)
    {
        return new IniciarSesionResult(
            false,
            mensaje);
    }
}
