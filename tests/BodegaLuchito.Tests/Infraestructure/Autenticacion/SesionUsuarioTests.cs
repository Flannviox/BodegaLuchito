using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Autenticacion.Enums;

namespace BodegaLuchito.Tests.Application.Autenticacion;

public sealed class SesionUsuarioTests
{
    [Fact]
    public void CerrarSesion_DebeEliminarUsuarioActual()
    {
        var sesion =
            new SesionUsuario();

        sesion.IniciarSesion(
            new UsuarioSesion
            {
                IdUsuario = 1,
                NombreCompleto = "Administradora",
                NombreUsuario = "admin",
                Rol = RolUsuario.Administradora
            });

        Assert.True(sesion.EstaAutenticado);

        sesion.CerrarSesion();

        Assert.False(sesion.EstaAutenticado);
        Assert.Null(sesion.UsuarioActual);
    }
}
