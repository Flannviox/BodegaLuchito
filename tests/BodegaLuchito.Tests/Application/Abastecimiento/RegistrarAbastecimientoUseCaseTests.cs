using System;
using System.Linq;
using System.Threading.Tasks;
using BodegaLuchito.Application.Abastecimiento.DTOs;
using BodegaLuchito.Application.Abastecimiento.UseCases;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Inventario.Entities;
using BodegaLuchito.Domain.Inventario.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Infrastructure.Abastecimiento.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Tests.Application.Abastecimiento;

public sealed class RegistrarAbastecimientoUseCaseTests
{
    [Fact]
    public async Task RegistrarConCostoTotalLote_CreaEntradaInventarioYEgresoCaja()
    {
        var (connection, options) = await CrearBaseEnMemoriaAsync();
        await using var connectionDispose = connection;
        var factory = new TestDbContextFactory(options);
        var datos = await SembrarDatosValidosAsync(options);
        var useCase = new RegistrarAbastecimientoUseCase(new AbastecimientoRepository(factory));

        await useCase.ExecuteAsync(new RegistrarAbastecimientoRequest
        {
            ProveedorId = datos.proveedorId,
            UsuarioId = datos.usuarioId,
            MetodoPago = MetodoPago.Efectivo,
            Detalles =
            [
                new DetalleAbastecimientoRequest
                {
                    ProductoId = datos.productoId,
                    Cantidad = 15m,
                    CostoUnitario = 2.6667m,
                    TotalLinea = 40m
                }
            ]
        });

        await using var context = new BodegaLuchitoDbContext(options);
        var abastecimiento = await context.Set<EntidadAbastecimiento>()
            .Include(x => x.Detalles)
            .SingleAsync();
        var producto = await context.Set<Producto>().SingleAsync();
        var movimientoInventario = await context.Set<MovimientoInventario>().SingleAsync();
        var movimientoCaja = await context.Set<MovimientoCaja>().SingleAsync();

        Assert.Equal(40m, abastecimiento.Total);
        Assert.Equal(2.6667m, abastecimiento.Detalles.Single().CostoUnitario);
        Assert.Equal(40m, abastecimiento.Detalles.Single().TotalLinea);

        Assert.Equal(25m, producto.StockActual);
        Assert.Equal(TipoMovimientoInventario.EntradaAbastecimiento, movimientoInventario.Tipo);
        Assert.Equal(15m, movimientoInventario.Cantidad);
        Assert.Equal(10m, movimientoInventario.StockAnterior);
        Assert.Equal(25m, movimientoInventario.StockPosterior);
        Assert.Equal(abastecimiento.Id, movimientoInventario.AbastecimientoId);

        Assert.Equal(TipoMovimientoCaja.EgresoAbastecimiento, movimientoCaja.Tipo);
        Assert.Equal(MetodoPago.Efectivo, movimientoCaja.MetodoPago);
        Assert.Equal(40m, movimientoCaja.Monto);
        Assert.Equal(abastecimiento.Id, movimientoCaja.AbastecimientoId);

        var historial = await new ConsultarHistorialAbastecimientosUseCase(
            new AbastecimientoRepository(factory)).ExecuteAsync();

        var registroHistorico = Assert.Single(historial);
        Assert.Equal("Proveedor Prueba", registroHistorico.Proveedor.Nombre);
        Assert.Equal(MetodoPago.Efectivo, registroHistorico.MetodoPago);
        Assert.Equal(40m, registroHistorico.Total);
        Assert.Equal("Gaseosa personal", registroHistorico.Detalles.Single().Producto.Nombre);
        Assert.Equal(2.6667m, registroHistorico.Detalles.Single().CostoUnitario);
    }

    [Fact]
    public async Task RegistrarConProductoInactivo_NoDejaRegistrosParciales()
    {
        var (connection, options) = await CrearBaseEnMemoriaAsync();
        await using var connectionDispose = connection;
        var factory = new TestDbContextFactory(options);
        var datos = await SembrarDatosValidosAsync(options, productoActivo: false);
        var useCase = new RegistrarAbastecimientoUseCase(new AbastecimientoRepository(factory));

        var action = () => useCase.ExecuteAsync(new RegistrarAbastecimientoRequest
        {
            ProveedorId = datos.proveedorId,
            UsuarioId = datos.usuarioId,
            MetodoPago = MetodoPago.Yape,
            Detalles =
            [
                new DetalleAbastecimientoRequest
                {
                    ProductoId = datos.productoId,
                    Cantidad = 2m,
                    CostoUnitario = 4m,
                    TotalLinea = 8m
                }
            ]
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("No se puede registrar un abastecimiento con productos inactivos.", exception.Message);

        await using var context = new BodegaLuchitoDbContext(options);
        Assert.Empty(await context.Set<EntidadAbastecimiento>().ToListAsync());
        Assert.Empty(await context.Set<MovimientoInventario>().ToListAsync());
        Assert.Empty(await context.Set<MovimientoCaja>().ToListAsync());
        Assert.Equal(10m, (await context.Set<Producto>().SingleAsync()).StockActual);
    }

    [Fact]
    public async Task RegistrarConProductoAntiguoSinMarcaInventariable_ActualizaSuStock()
    {
        var (connection, options) = await CrearBaseEnMemoriaAsync();
        await using var connectionDispose = connection;
        var factory = new TestDbContextFactory(options);
        var datos = await SembrarDatosValidosAsync(options, controlaInventario: false);
        var useCase = new RegistrarAbastecimientoUseCase(new AbastecimientoRepository(factory));

        await useCase.ExecuteAsync(new RegistrarAbastecimientoRequest
        {
            ProveedorId = datos.proveedorId,
            UsuarioId = datos.usuarioId,
            MetodoPago = MetodoPago.Efectivo,
            Detalles =
            [
                new DetalleAbastecimientoRequest
                {
                    ProductoId = datos.productoId,
                    Cantidad = 5m,
                    CostoUnitario = 2m,
                    TotalLinea = 10m
                }
            ]
        });

        await using var context = new BodegaLuchitoDbContext(options);
        Assert.Equal(15m, (await context.Set<Producto>().SingleAsync()).StockActual);
        Assert.Single(await context.Set<MovimientoInventario>().ToListAsync());
    }

    [Fact]
    public async Task RegistrarConProductoPorPeso_AceptaCantidadDecimal()
    {
        var (connection, options) = await CrearBaseEnMemoriaAsync();
        await using var connectionDispose = connection;
        var factory = new TestDbContextFactory(options);
        var datos = await SembrarDatosValidosAsync(options, unidadVenta: UnidadVenta.Peso);
        var useCase = new RegistrarAbastecimientoUseCase(new AbastecimientoRepository(factory));

        await useCase.ExecuteAsync(new RegistrarAbastecimientoRequest
        {
            ProveedorId = datos.proveedorId,
            UsuarioId = datos.usuarioId,
            MetodoPago = MetodoPago.Efectivo,
            Detalles =
            [
                new DetalleAbastecimientoRequest
                {
                    ProductoId = datos.productoId,
                    Cantidad = 1.25m,
                    CostoUnitario = 4m,
                    TotalLinea = 5m
                }
            ]
        });

        await using var context = new BodegaLuchitoDbContext(options);
        var producto = await context.Set<Producto>().SingleAsync();
        var movimiento = await context.Set<MovimientoInventario>().SingleAsync();

        Assert.Equal(11.25m, producto.StockActual);
        Assert.Equal(1.25m, movimiento.Cantidad);
    }

    [Fact]
    public async Task RegistrarConProductoPorUnidadYCantidadDecimal_NoDejaRegistrosParciales()
    {
        var (connection, options) = await CrearBaseEnMemoriaAsync();
        await using var connectionDispose = connection;
        var factory = new TestDbContextFactory(options);
        var datos = await SembrarDatosValidosAsync(options);
        var useCase = new RegistrarAbastecimientoUseCase(new AbastecimientoRepository(factory));

        var action = () => useCase.ExecuteAsync(new RegistrarAbastecimientoRequest
        {
            ProveedorId = datos.proveedorId,
            UsuarioId = datos.usuarioId,
            MetodoPago = MetodoPago.Efectivo,
            Detalles =
            [
                new DetalleAbastecimientoRequest
                {
                    ProductoId = datos.productoId,
                    Cantidad = 1.5m,
                    CostoUnitario = 4m,
                    TotalLinea = 6m
                }
            ]
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Equal("Los productos por unidad solo admiten cantidades enteras.", exception.Message);

        await using var context = new BodegaLuchitoDbContext(options);
        Assert.Empty(await context.Set<EntidadAbastecimiento>().ToListAsync());
        Assert.Empty(await context.Set<MovimientoInventario>().ToListAsync());
        Assert.Empty(await context.Set<MovimientoCaja>().ToListAsync());
    }

    [Fact]
    public async Task CompraConDosNuevosSinCodigoYUnoExistente_GuardaTodoUnaVez()
    {
        var (connection, options) = await CrearBaseEnMemoriaAsync();
        await using var conexion = connection;
        var datos = await SembrarDatosValidosAsync(options);
        await using var contexto = new BodegaLuchitoDbContext(options);
        var categoria = await contexto.Categorias.SingleAsync();
        var nuevo = new BodegaLuchito.Application.Productos.DTOs.RegistrarProductoRequest(
            "Producto nuevo", categoria.Id, null, 6m, UnidadVenta.Peso, true, 0, 2);
        var solicitud = new RegistrarAbastecimientoRequest {
            UsuarioId = datos.usuarioId, ProveedorId = datos.proveedorId, MetodoPago = MetodoPago.Efectivo,
            Detalles = [
                new() { ProductoNuevo = nuevo, Cantidad = 1.125m, CostoUnitario = 4, TotalLinea = 4.50m },
                new() { ProductoNuevo = nuevo with { Nombre = "Segundo nuevo" }, Cantidad = 2, CostoUnitario = 4, TotalLinea = 8 },
                new() { ProductoId = datos.productoId, Cantidad = 3, CostoUnitario = 2, TotalLinea = 6 }
            ]
        };
        var caso = new RegistrarAbastecimientoUseCase(new AbastecimientoRepository(new TestDbContextFactory(options)));
        await caso.ExecuteAsync(solicitud);
        var productos = await contexto.Productos.AsNoTracking().ToListAsync();
        Assert.Equal(3, productos.Count);
        Assert.Equal(1.125m, productos.Single(x => x.Nombre == "Producto nuevo").StockActual);
        Assert.Equal(2m, productos.Single(x => x.Nombre == "Segundo nuevo").StockActual);
        Assert.Equal(13m, productos.Single(x => x.Id == datos.productoId).StockActual);
        Assert.Equal(3, await contexto.MovimientosInventario.CountAsync());
        Assert.Equal(18.5m, (await contexto.Set<MovimientoCaja>().SingleAsync()).Monto);
        Assert.Single(await contexto.Abastecimientos.ToListAsync());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CompraNuevaFallida_NoDejaProductosHuerfanos(bool cajaCerrada)
    {
        var (connection, options) = await CrearBaseEnMemoriaAsync();
        await using var conexion = connection;
        var datos = await SembrarDatosValidosAsync(options);
        await using var contexto = new BodegaLuchitoDbContext(options);
        var categoria = await contexto.Categorias.SingleAsync();
        if (cajaCerrada) {
            (await contexto.Set<SesionCaja>().SingleAsync()).Estado = EstadoSesionCaja.Cerrada;
            await contexto.SaveChangesAsync();
        }
        var nuevo = new BodegaLuchito.Application.Productos.DTOs.RegistrarProductoRequest(
            "Producto nuevo", categoria.Id, "CODIGO-REPETIDO", 6m, UnidadVenta.Unidad, true, 0, 2);
        var solicitud = new RegistrarAbastecimientoRequest {
            UsuarioId = datos.usuarioId, ProveedorId = datos.proveedorId, MetodoPago = MetodoPago.Efectivo,
            Detalles = [
                new() { ProductoNuevo = nuevo, Cantidad = 2, CostoUnitario = 4, TotalLinea = 8 },
                new() { ProductoNuevo = nuevo with { Nombre = "Otro nombre" }, Cantidad = 2, CostoUnitario = 4, TotalLinea = 8 }
            ]
        };
        var caso = new RegistrarAbastecimientoUseCase(new AbastecimientoRepository(new TestDbContextFactory(options)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => caso.ExecuteAsync(solicitud));
        Assert.Single(await contexto.Productos.AsNoTracking().ToListAsync());
        Assert.Empty(await contexto.Abastecimientos.ToListAsync());
        Assert.Empty(await contexto.MovimientosInventario.ToListAsync());
        Assert.Empty(await contexto.Set<MovimientoCaja>().ToListAsync());
    }

    private static async Task<(SqliteConnection connection, DbContextOptions<BodegaLuchitoDbContext> options)>
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

    private static async Task<(int usuarioId, int proveedorId, int productoId)> SembrarDatosValidosAsync(
        DbContextOptions<BodegaLuchitoDbContext> options,
        bool productoActivo = true,
        UnidadVenta unidadVenta = UnidadVenta.Unidad,
        bool controlaInventario = true)
    {
        await using var context = new BodegaLuchitoDbContext(options);
        var usuario = new Usuario
        {
            NombreCompleto = "Administradora Prueba",
            NombreUsuario = "admin",
            PasswordHash = "hash-prueba",
            Rol = RolUsuario.Administradora,
            Activo = true
        };
        var proveedor = new Proveedor
        {
            Nombre = "Proveedor Prueba",
            Ruc = "12345678901",
            Activo = true
        };
        var categoria = new Categoria { Nombre = "Bebidas" };

        context.AddRange(usuario, proveedor, categoria);
        await context.SaveChangesAsync();

        var producto = new Producto
        {
            Nombre = "Gaseosa personal",
            CategoriaId = categoria.Id,
            PrecioVenta = 3m,
            UnidadVenta = unidadVenta,
            ControlaInventario = controlaInventario,
            StockActual = 10m,
            StockMinimo = 2m,
            Activo = productoActivo
        };
        var sesion = new SesionCaja
        {
            UsuarioAperturaId = usuario.Id,
            FechaApertura = DateTime.Now,
            FondoInicial = 50m,
            Estado = EstadoSesionCaja.Abierta
        };

        context.AddRange(producto, sesion);
        await context.SaveChangesAsync();

        return (usuario.Id, proveedor.Id, producto.Id);
    }
}
