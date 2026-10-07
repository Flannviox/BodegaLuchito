using BodegaLuchito.Application.Ventas.DTOs;
using BodegaLuchito.Application.Ventas.UseCases;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Ventas.Repositories;
using BodegaLuchito.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using BodegaLuchito.Domain.Caja.Enums;
using Xunit;

namespace BodegaLuchito.Tests.Application.Ventas;

public sealed class VentaCajaTests
{
    private static async Task<(
        SqliteConnection connection,
        DbContextOptions<BodegaLuchitoDbContext> options)>
        CrearBaseEnMemoriaAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new BodegaLuchitoDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return (connection, options);
    }

    private static async Task<(int usuarioId, int productoId)>
        SembrarDatosSinCajaAsync(
            DbContextOptions<BodegaLuchitoDbContext> options)
    {
        await using var context = new BodegaLuchitoDbContext(options);

        var usuario = new Usuario
        {
            NombreCompleto = "Vendedora Prueba",
            NombreUsuario = "vendedora-caja",
            PasswordHash = "hash-prueba",
            Rol = RolUsuario.Vendedora,
            Activo = true
        };

        var categoria = new Categoria
        {
            Nombre = "Abarrotes"
        };

        var producto = new Producto
        {
            Nombre = "Producto Prueba",
            Categoria = categoria,
            PrecioVenta = 5m,
            UnidadVenta = UnidadVenta.Unidad,
            ControlaInventario = true,
            StockActual = 10m,
            StockMinimo = 2m,
            Activo = true
        };

        context.Set<Usuario>().Add(usuario);
        context.Set<Categoria>().Add(categoria);
        context.Set<Producto>().Add(producto);

        await context.SaveChangesAsync();

        return (usuario.Id, producto.Id);
    }

    private static async Task<(
    int usuarioId,
    int productoId,
    int sesionCajaId)>
    SembrarDatosConCajaAsync(
        DbContextOptions<BodegaLuchitoDbContext> options)
    {
        var (usuarioId, productoId) =
            await SembrarDatosSinCajaAsync(options);

        await using var context =
            new BodegaLuchitoDbContext(options);

        var sesionCaja = new SesionCaja
        {
            UsuarioAperturaId = usuarioId,
            FechaApertura = DateTime.Now,
            FondoInicial = 100m,
            Estado = EstadoSesionCaja.Abierta
        };

        context.Set<SesionCaja>().Add(sesionCaja);

        await context.SaveChangesAsync();

        return (
            usuarioId,
            productoId,
            sesionCaja.Id);
    }

    [Fact]
    public async Task VentaConCajaAbierta_RegistraIngresoEnCaja()
    {
        var (connection, options) =
            await CrearBaseEnMemoriaAsync();

        await using var connectionDispose = connection;

        var (usuarioId, productoId, sesionCajaId) =
            await SembrarDatosConCajaAsync(options);

        var factory =
            new TestDbContextFactory(options);

        var repository =
            new VentaRepository(factory);

        var useCase =
            new RegistrarVentaUseCase(repository);

        var request = new RegistrarVentaRequest
        {
            UsuarioId = usuarioId,
            Subtotal = 5m,
            IGV = 0m,
            Total = 5m,
            MetodoPago = MetodoPago.Yape,
            Detalles =
            [
                new DetalleVentaRequest
            {
                ProductoId = productoId,
                Cantidad = 1m,
                PrecioUnitario = 5m,
                Subtotal = 5m
            }
            ]
        };

        var ventaId =
            await useCase.ExecuteAsync(request);

        await using var context =
            new BodegaLuchitoDbContext(options);

        var movimiento =
            await context.Set<MovimientoCaja>()
                .SingleAsync();

        Assert.Equal(
            sesionCajaId,
            movimiento.SesionCajaId);

        Assert.Equal(
            usuarioId,
            movimiento.UsuarioId);

        Assert.Equal(
            TipoMovimientoCaja.IngresoVenta,
            movimiento.Tipo);

        Assert.Equal(
            MetodoPago.Yape,
            movimiento.MetodoPago);

        Assert.Equal(
            5m,
            movimiento.Monto);

        Assert.Equal(
            ventaId,
            movimiento.VentaId);

        Assert.Null(
            movimiento.AbastecimientoId);
    }

    [Fact]
    public async Task VentaSinCajaAbierta_NoGuardaNada()
    {
        var (connection, options) =
            await CrearBaseEnMemoriaAsync();

        await using var connectionDispose = connection;

        var (usuarioId, productoId) =
            await SembrarDatosSinCajaAsync(options);

        var factory = new TestDbContextFactory(options);

        var repository = new VentaRepository(factory);

        var useCase = new RegistrarVentaUseCase(repository);

        var request = new RegistrarVentaRequest
        {
            UsuarioId = usuarioId,
            Subtotal = 5m,
            IGV = 0m,
            Total = 5m,
            MetodoPago = MetodoPago.Efectivo,
            Detalles =
            [
                new DetalleVentaRequest
                {
                    ProductoId = productoId,
                    Cantidad = 1m,
                    PrecioUnitario = 5m,
                    Subtotal = 5m
                }
            ]
        };

        var exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => useCase.ExecuteAsync(request));

        Assert.Contains(
            "sesión de caja",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        await using var context =
            new BodegaLuchitoDbContext(options);

        Assert.Empty(
            await context.Set<Venta>().ToListAsync());

        Assert.Empty(
            await context.Set<DetalleVenta>().ToListAsync());

        Assert.Empty(
            await context.Set<MovimientoCaja>().ToListAsync());

        Assert.Empty(
            await context.MovimientosInventario.ToListAsync());

        var producto =
            await context.Productos.FindAsync(productoId);

        Assert.NotNull(producto);
        Assert.Equal(10m, producto.StockActual);
    }

    [Fact]
    public async Task SiFallaMovimientoCaja_RevierteVentaInventarioYCaja()
    {
        var (connection, options) =
            await CrearBaseEnMemoriaAsync();

        await using var connectionDispose = connection;

        var (usuarioId, productoId, _) =
            await SembrarDatosConCajaAsync(options);

        await using (var context =
            new BodegaLuchitoDbContext(options))
        {
            await context.Database.ExecuteSqlRawAsync(
                """
            CREATE TRIGGER FallarIngresoCaja
            BEFORE INSERT ON MovimientosCaja
            BEGIN
                SELECT RAISE(ABORT, 'Fallo simulado de caja');
            END;
            """);
        }

        var factory =
            new TestDbContextFactory(options);

        var repository =
            new VentaRepository(factory);

        var useCase =
            new RegistrarVentaUseCase(repository);

        var request = new RegistrarVentaRequest
        {
            UsuarioId = usuarioId,
            Subtotal = 5m,
            IGV = 0m,
            Total = 5m,
            MetodoPago = MetodoPago.Efectivo,
            Detalles =
            [
                new DetalleVentaRequest
            {
                ProductoId = productoId,
                Cantidad = 1m,
                PrecioUnitario = 5m,
                Subtotal = 5m
            }
            ]
        };

        await Assert.ThrowsAsync<DbUpdateException>(
            () => useCase.ExecuteAsync(request));

        await using var comprobacion =
            new BodegaLuchitoDbContext(options);

        Assert.Empty(
            await comprobacion.Set<Venta>().ToListAsync());

        Assert.Empty(
            await comprobacion.Set<DetalleVenta>().ToListAsync());

        Assert.Empty(
            await comprobacion.MovimientosInventario.ToListAsync());

        Assert.Empty(
            await comprobacion.Set<MovimientoCaja>().ToListAsync());

        var producto =
            await comprobacion.Productos.FindAsync(productoId);

        Assert.NotNull(producto);

        Assert.Equal(
            10m,
            producto.StockActual);
    }
}
