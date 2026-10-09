using BodegaLuchito.Application.Caja.UseCases;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Infrastructure.Caja.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Tests.Application.Caja;

public class ResolverDescuadreCajaUseCaseTests
{
    private static async Task<(
        SqliteConnection connection,
        DbContextOptions<BodegaLuchitoDbContext> options)>
        CrearBaseEnMemoriaAsync()
    {
        var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var context =
            new BodegaLuchitoDbContext(options);

        await context.Database.EnsureCreatedAsync();

        return (connection, options);
    }

    private static SesionUsuario CrearSesionAdministradora()
    {
        var sesionUsuario =
            new SesionUsuario();

        sesionUsuario.IniciarSesion(
            new UsuarioSesion
            {
                IdUsuario = 99,
                NombreCompleto = "Administradora",
                NombreUsuario = "admin",
                Rol = RolUsuario.Administradora
            });

        return sesionUsuario;
    }

    [Fact]
    public async Task ResolverDescuadre_DebeGuardarResolucion()
    {
        var (connection, options) =
            await CrearBaseEnMemoriaAsync();

        await using var connectionDispose = connection;

        await using var context =
            new BodegaLuchitoDbContext(options);

        var sesion = new SesionCaja
        {
            UsuarioAperturaId = 1,
            FechaApertura = DateTime.Now.AddHours(-8),
            FechaCierre = DateTime.Now,
            FondoInicial = 100m,
            Estado = EstadoSesionCaja.Cerrada,
            EfectivoEsperado = 150m,
            EfectivoReal = 140m,
            DiferenciaEfectivo = -10m
        };

        context.Set<SesionCaja>().Add(sesion);
        await context.SaveChangesAsync();

        var repository =
            new CajaRepository(context);

        var sesionUsuario =
            CrearSesionAdministradora();

        var useCase =
            new ResolverDescuadreCajaUseCase(
                repository,
                sesionUsuario);

        await useCase.EjecutarAsync(
            sesion.Id,
            "Descuento aplicado.");

        context.ChangeTracker.Clear();

        var sesionGuardada =
            await context.Set<SesionCaja>()
                .SingleAsync(
                    s => s.Id == sesion.Id);

        Assert.True(
            sesionGuardada.DescuadreResuelto);

        Assert.NotNull(
            sesionGuardada.FechaResolucionDescuadre);

        Assert.Equal(
            99,
            sesionGuardada.UsuarioResolucionId);

        Assert.Equal(
            "Descuento aplicado.",
            sesionGuardada.ObservacionResolucionDescuadre);
    }

    [Fact]
    public async Task ResolverDescuadre_YaResuelto_DebeSerRechazado()
    {
        var (connection, options) =
            await CrearBaseEnMemoriaAsync();

        await using var connectionDispose = connection;

        await using var context =
            new BodegaLuchitoDbContext(options);

        var sesion = new SesionCaja
        {
            UsuarioAperturaId = 1,
            FechaApertura = DateTime.Now.AddHours(-8),
            FechaCierre = DateTime.Now,
            FondoInicial = 100m,
            Estado = EstadoSesionCaja.Cerrada,
            EfectivoEsperado = 150m,
            EfectivoReal = 140m,
            DiferenciaEfectivo = -10m
        };

        context.Set<SesionCaja>().Add(sesion);
        await context.SaveChangesAsync();

        var repository =
            new CajaRepository(context);

        var sesionUsuario =
            CrearSesionAdministradora();

        var useCase =
            new ResolverDescuadreCajaUseCase(
                repository,
                sesionUsuario);

        await useCase.EjecutarAsync(
            sesion.Id,
            "Primera resolución.");

        var excepcion =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => useCase.EjecutarAsync(
                    sesion.Id,
                    "Segunda resolución."));

        Assert.Contains(
            "ya fue marcado como resuelto",
            excepcion.Message);
    }

    [Fact]
    public async Task ResolverDescuadre_SinDiferencia_DebeSerRechazado()
    {
        var (connection, options) =
            await CrearBaseEnMemoriaAsync();

        await using var connectionDispose = connection;

        await using var context =
            new BodegaLuchitoDbContext(options);

        var sesion = new SesionCaja
        {
            UsuarioAperturaId = 1,
            FechaApertura = DateTime.Now.AddHours(-8),
            FechaCierre = DateTime.Now,
            FondoInicial = 100m,
            Estado = EstadoSesionCaja.Cerrada,
            EfectivoEsperado = 150m,
            EfectivoReal = 150m,
            DiferenciaEfectivo = 0m
        };

        context.Set<SesionCaja>().Add(sesion);
        await context.SaveChangesAsync();

        var repository =
            new CajaRepository(context);

        var sesionUsuario =
            CrearSesionAdministradora();

        var useCase =
            new ResolverDescuadreCajaUseCase(
                repository,
                sesionUsuario);

        var excepcion =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => useCase.EjecutarAsync(
                    sesion.Id,
                    null));

        Assert.Contains(
            "no presenta un descuadre",
            excepcion.Message);

        context.ChangeTracker.Clear();

        var sesionGuardada =
            await context.Set<SesionCaja>()
                .SingleAsync(
                    s => s.Id == sesion.Id);

        Assert.False(
            sesionGuardada.DescuadreResuelto);

        Assert.Null(
            sesionGuardada.FechaResolucionDescuadre);

        Assert.Null(
            sesionGuardada.UsuarioResolucionId);
    }

    [Fact]
    public async Task ResolverDescuadre_UsuarioNoAdministrador_DebeSerRechazado()
    {
        var (connection, options) =
            await CrearBaseEnMemoriaAsync();

        await using var connectionDispose = connection;

        await using var context =
            new BodegaLuchitoDbContext(options);

        var sesion = new SesionCaja
        {
            UsuarioAperturaId = 1,
            FechaApertura = DateTime.Now.AddHours(-8),
            FechaCierre = DateTime.Now,
            FondoInicial = 100m,
            Estado = EstadoSesionCaja.Cerrada,
            EfectivoEsperado = 150m,
            EfectivoReal = 140m,
            DiferenciaEfectivo = -10m
        };

        context.Set<SesionCaja>().Add(sesion);
        await context.SaveChangesAsync();

        var repository =
            new CajaRepository(context);

        var sesionUsuario =
            new SesionUsuario();

        sesionUsuario.IniciarSesion(
            new UsuarioSesion
            {
                IdUsuario = 50,
                NombreCompleto = "Vendedora",
                NombreUsuario = "vendedora",
                Rol = RolUsuario.Vendedora
            });

        var useCase =
            new ResolverDescuadreCajaUseCase(
                repository,
                sesionUsuario);

        var excepcion =
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => useCase.EjecutarAsync(
                    sesion.Id,
                    "Intento sin permisos."));

        Assert.Contains(
            "Solo una administradora",
            excepcion.Message);

        context.ChangeTracker.Clear();

        var sesionGuardada =
            await context.Set<SesionCaja>()
                .SingleAsync(
                    s => s.Id == sesion.Id);

        Assert.False(
            sesionGuardada.DescuadreResuelto);

        Assert.Null(
            sesionGuardada.FechaResolucionDescuadre);

        Assert.Null(
            sesionGuardada.UsuarioResolucionId);
    }
}
