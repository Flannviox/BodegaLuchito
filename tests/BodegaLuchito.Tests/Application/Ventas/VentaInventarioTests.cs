using BodegaLuchito.Application.Inventario.DTOs;
using BodegaLuchito.Application.Ventas.DTOs;
using BodegaLuchito.Application.Ventas.UseCases;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Domain.Inventario.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Infrastructure.Inventario.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Ventas.Repositories;
using BodegaLuchito.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Tests.Application.Ventas;

public sealed class VentaInventarioTests : IAsyncLifetime
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"venta-inventario-{Guid.NewGuid():N}.db");
    private DbContextOptions<BodegaLuchitoDbContext> _opciones = null!;
    private RegistrarVentaUseCase _registrar = null!;
    public async Task InitializeAsync()
    {
        _opciones = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite($"Data Source={_ruta};Pooling=False").Options;
        await using var db = new BodegaLuchitoDbContext(_opciones);
        await db.Database.MigrateAsync();
        db.Add(new Usuario { Id=1, NombreCompleto="Vendedora", NombreUsuario="venta", PasswordHash="prueba",
            Rol=RolUsuario.Vendedora, Activo=true });
        db.Categorias.Add(new Categoria { Id=1, Nombre="Abarrotes" });
        db.Productos.AddRange(
            new Producto { Id=1, Nombre="Botella", CategoriaId=1, UnidadVenta=UnidadVenta.Unidad,
                PrecioVenta=3, StockActual=10, ControlaInventario=true },
            new Producto { Id=2, Nombre="Papa", CategoriaId=1, UnidadVenta=UnidadVenta.Peso,
                PrecioVenta=3, StockActual=5.625m, ControlaInventario=true });
        await db.SaveChangesAsync();
        _registrar = new RegistrarVentaUseCase(new VentaRepository(new TestDbContextFactory(_opciones)));
    }
    public Task DisposeAsync()
    {
        if (File.Exists(_ruta)) File.Delete(_ruta);
        return Task.CompletedTask;
    }
    private static RegistrarVentaRequest Solicitud(params (int id, decimal cantidad)[] lineas)
    {
        var total = lineas.Sum(x => Math.Round(x.cantidad * 3, 2));
        return new RegistrarVentaRequest {
            UsuarioId=1, Subtotal=total, Total=total,
            Detalles=lineas.Select(x => new DetalleVentaRequest {
                ProductoId=x.id, Cantidad=x.cantidad, PrecioUnitario=3, Subtotal=Math.Round(x.cantidad * 3, 2)
            }).ToList()
        };
    }

    [Fact]
    public async Task VentaMixta_DescuentaYConservaHistorialAlReabrir()
    {
        var id = await _registrar.ExecuteAsync(Solicitud((1,3), (2,1.125m)));
        await using var db = new BodegaLuchitoDbContext(_opciones);
        Assert.Equal(7m, (await db.Productos.FindAsync(1))!.StockActual);
        Assert.Equal(4.5m, (await db.Productos.FindAsync(2))!.StockActual);
        Assert.Single(await db.Set<Venta>().ToListAsync());
        var historial = await new InventarioRepository(new TestDbContextFactory(_opciones)).ObtenerHistorialCompletoAsync();
        Assert.Equal(2, historial.Count);
        Assert.All(historial, x => {
            Assert.Equal("Venta", x.TipoDescripcion);
            Assert.Equal($"Salida por venta #{id}", x.Motivo);
            Assert.Equal("Vendedora", x.Responsable);
            Assert.Null(x.AbastecimientoId);
        });
        Assert.Equal(-3m, historial.Single(x => x.ProductoId==1).Variacion);
        Assert.Equal(-1.125m, historial.Single(x => x.ProductoId==2).Variacion);
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Theory]
    [InlineData(1, 11, typeof(InvalidOperationException))]
    [InlineData(1, 0.5, typeof(ArgumentException))]
    [InlineData(2, 1.0001, typeof(ArgumentException))]
    [InlineData(99, 1, typeof(InvalidOperationException))]
    public async Task VentaInvalida_NoGuardaNada(int producto, double cantidad, Type error)
    {
        var ex = await Record.ExceptionAsync(() => _registrar.ExecuteAsync(Solicitud((producto,(decimal)cantidad))));
        Assert.IsType(error, ex);
        await ComprobarSinCambiosAsync();
    }

    [Fact]
    public async Task LineasRepetidas_SeValidanSumadas()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _registrar.ExecuteAsync(Solicitud((1,6),(1,5))));
        await ComprobarSinCambiosAsync();
        await _registrar.ExecuteAsync(Solicitud((1,2),(1,3)));
        await using var db = new BodegaLuchitoDbContext(_opciones);
        var movimiento = await db.MovimientosInventario.SingleAsync();
        Assert.Equal(5m, movimiento.Cantidad);
        Assert.Equal(10m, movimiento.StockAnterior);
        Assert.Equal(5m, movimiento.StockPosterior);
    }

    [Fact]
    public async Task SiFallaUnProducto_NoDescuentaLosDemas()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _registrar.ExecuteAsync(Solicitud((1,2),(2,6))));
        await ComprobarSinCambiosAsync();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ProductoOUsuarioDesactivado_RechazaVenta(bool producto)
    {
        await using (var db = new BodegaLuchitoDbContext(_opciones))
        {
            if (producto) (await db.Productos.FindAsync(1))!.Activo=false;
            else (await db.Set<Usuario>().FindAsync(1))!.Activo=false;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => _registrar.ExecuteAsync(Solicitud((1,1))));
        await ComprobarSinCambiosAsync();
    }

    [Fact]
    public async Task FalloAlInsertarMovimiento_RevierteVentaDetallesYStock()
    {
        await using (var db = new BodegaLuchitoDbContext(_opciones))
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TRIGGER FallarSalida BEFORE INSERT ON MovimientosInventario
                WHEN NEW.Tipo = 'Venta'
                BEGIN SELECT RAISE(ABORT, 'Fallo simulado de inventario'); END;
                """);
        await Assert.ThrowsAsync<DbUpdateException>(() => _registrar.ExecuteAsync(Solicitud((1,2))));
        await ComprobarSinCambiosAsync();
    }

    [Fact]
    public async Task SegundaVentaConsultaStockActual_YNoPermiteSaldoInicialAlAgotar()
    {
        // Incluso los productos antiguos sin la marca inventariable deben descontarse.
        await using (var db = new BodegaLuchitoDbContext(_opciones))
        {
            (await db.Productos.FindAsync(1))!.ControlaInventario=false;
            await db.SaveChangesAsync();
        }
        await _registrar.ExecuteAsync(Solicitud((1,10)));
        await Assert.ThrowsAsync<InvalidOperationException>(() => _registrar.ExecuteAsync(Solicitud((1,1))));
        var repo = new InventarioRepository(new TestDbContextFactory(_opciones));
        var stock = (await repo.ObtenerExistenciasAsync()).Single(x => x.ProductoId==1);
        Assert.Equal(0m, stock.StockActual);
        Assert.False(stock.PuedeRegistrarSaldoInicial);
        Assert.Single(await repo.ObtenerHistorialCompletoAsync());
    }

    [Fact]
    public async Task DosVentasCompitiendoPorElStock_NoLoDejanNegativo()
    {
        async Task<bool> VenderAsync()
        {
            try { await _registrar.ExecuteAsync(Solicitud((1,8))); return true; }
            catch (InvalidOperationException) { return false; }
        }
        var resultados = await Task.WhenAll(Task.Run(VenderAsync), Task.Run(VenderAsync));
        Assert.Single(resultados, x => x);
        await using var db = new BodegaLuchitoDbContext(_opciones);
        Assert.Equal(2m, (await db.Productos.FindAsync(1))!.StockActual);
        Assert.Single(await db.Set<Venta>().ToListAsync());
        Assert.Single(await db.MovimientosInventario.ToListAsync());
    }

    [Fact]
    public async Task VentaAnteriorSinMovimiento_ImpideRegistrarOtroSaldoInicial()
    {
        await using (var db = new BodegaLuchitoDbContext(_opciones))
        {
            (await db.Productos.FindAsync(1))!.StockActual=0;
            db.Add(new Venta {UsuarioId=1, Total=30, Subtotal=30,
                Detalles=[new DetalleVenta {ProductoId=1, Cantidad=10, PrecioUnitario=3, Subtotal=30}]});
            await db.SaveChangesAsync();
        }
        var repo = new InventarioRepository(new TestDbContextFactory(_opciones));
        Assert.False((await repo.ObtenerExistenciasAsync()).Single(x => x.ProductoId==1).PuedeRegistrarSaldoInicial);
        await using (var db = new BodegaLuchitoDbContext(_opciones))
        {
            (await db.Set<Usuario>().FindAsync(1))!.Rol=RolUsuario.Administradora;
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.RegistrarMovimientoManualAsync(
            new RegistrarMovimientoManualRequest(1, TipoMovimientoInventario.SaldoInicial, 5, 0, "Saldo repetido"), 1));
        await using var comprobar = new BodegaLuchitoDbContext(_opciones);
        Assert.Equal(0m, (await comprobar.Productos.FindAsync(1))!.StockActual);
        Assert.Empty(await comprobar.MovimientosInventario.ToListAsync());
    }

    private async Task ComprobarSinCambiosAsync()
    {
        await using var db = new BodegaLuchitoDbContext(_opciones);
        Assert.Empty(await db.Set<Venta>().ToListAsync());
        Assert.Empty(await db.Set<DetalleVenta>().ToListAsync());
        Assert.Empty(await db.MovimientosInventario.ToListAsync());
        Assert.Equal(10m, (await db.Productos.FindAsync(1))!.StockActual);
        Assert.Equal(5.625m, (await db.Productos.FindAsync(2))!.StockActual);
    }
}
