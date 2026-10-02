using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Application.Inventario.DTOs;
using BodegaLuchito.Application.Inventario.UseCases;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Inventario.Entities;
using BodegaLuchito.Domain.Inventario.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Infrastructure.Inventario.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Tests.Application.Inventario;

public sealed class InventarioOperativoTests : IAsyncLifetime
{
    private readonly string _ruta = Path.Combine(Path.GetTempPath(), $"inventario-test-{Guid.NewGuid():N}.db");
    private DbContextOptions<BodegaLuchitoDbContext> _opciones = null!;
    private GestionarInventarioUseCase _gestion = null!;
    private SesionUsuario _sesion = null!;

    public async Task InitializeAsync()
    {
        _opciones = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite($"Data Source={_ruta};Pooling=False").Options;
        await using var contexto = new BodegaLuchitoDbContext(_opciones);
        await contexto.Database.MigrateAsync();
        contexto.Set<Usuario>().Add(new Usuario { Id = 1, NombreCompleto = "Rocío",
            NombreUsuario = "admin", PasswordHash = "prueba", Rol = RolUsuario.Administradora });
        contexto.Categorias.Add(new Categoria { Id = 1, Nombre = "Abarrotes" });
        contexto.Productos.Add(new Producto { Id = 1, Nombre = "Arroz", CategoriaId = 1,
            UnidadVenta = UnidadVenta.Peso, ControlaInventario = true, StockMinimo = 5, PrecioVenta = 4 });
        contexto.Productos.Add(new Producto { Id = 2, Nombre = "Botella", CategoriaId = 1,
            UnidadVenta = UnidadVenta.Unidad, ControlaInventario = true, StockMinimo = 2, PrecioVenta = 3 });
        await contexto.SaveChangesAsync();
        _sesion = new SesionUsuario();
        _sesion.IniciarSesion(new UsuarioSesion { IdUsuario = 1, Rol = RolUsuario.Administradora });
        _gestion = new GestionarInventarioUseCase(new InventarioRepository(new TestDbContextFactory(_opciones)), _sesion);
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_ruta)) File.Delete(_ruta);
        return Task.CompletedTask;
    }

    private RegistrarMovimientoManualRequest Solicitud(TipoMovimientoInventario tipo, decimal cantidad,
        decimal esperado = 0, int producto = 1, string motivo = "Conteo de mercadería") =>
        new(producto, tipo, cantidad, esperado, motivo);

    [Fact]
    public async Task SaldoInicial_PersisteTrasReabrirYNoGeneraCaja()
    {
        await _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.SaldoInicial, 80.125m));
        var otraInstancia = new InventarioRepository(new TestDbContextFactory(_opciones));
        var existencia = (await otraInstancia.ObtenerExistenciasAsync()).Single(x => x.ProductoId == 1);
        Assert.Equal(80.125m, existencia.StockActual);
        Assert.False(existencia.PuedeRegistrarSaldoInicial);
        var movimiento = Assert.Single(await otraInstancia.ObtenerHistorialCompletoAsync());
        Assert.Equal("Rocío", movimiento.Responsable);
        Assert.Equal(80.125m, movimiento.Variacion);
        Assert.Equal("kg", movimiento.Unidad);
        await using var contexto = new BodegaLuchitoDbContext(_opciones);
        Assert.Empty(await contexto.Set<MovimientoCaja>().ToListAsync());
        Assert.Empty(await contexto.Abastecimientos.ToListAsync());
        Assert.False(contexto.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task ConteoYMerma_ConservanSecuenciaYNoPermitenRepetirSaldoInicial()
    {
        await _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.SaldoInicial, 8));
        await _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.AjusteConteo, 10.5m, 8));
        await _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.Merma, 10.5m, 10.5m));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.SaldoInicial, 4)));
        var historial = await _gestion.ConsultarHistorialAsync();
        Assert.Equal(3, historial.Count);
        Assert.Equal(0m, historial[0].StockPosterior);
        Assert.Equal(-10.5m, historial[0].Variacion);
        Assert.Equal(2.5m, historial[1].Variacion);
        Assert.False((await _gestion.ConsultarExistenciasAsync()).Single(x => x.ProductoId == 1).PuedeRegistrarSaldoInicial);
    }

    [Theory]
    [InlineData(TipoMovimientoInventario.SaldoInicial, 0, 1)]
    [InlineData(TipoMovimientoInventario.SaldoInicial, -1, 1)]
    [InlineData(TipoMovimientoInventario.SaldoInicial, 1.5, 2)]
    [InlineData(TipoMovimientoInventario.SaldoInicial, 0.0001, 1)]
    [InlineData(TipoMovimientoInventario.Merma, 1, 1)]
    [InlineData(TipoMovimientoInventario.AjusteConteo, 0, 1)]
    [InlineData(TipoMovimientoInventario.EntradaAbastecimiento, 2, 1)]
    public async Task CantidadOTipoInvalido_NoDejaCambios(TipoMovimientoInventario tipo, double cantidad, int producto)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _gestion.RegistrarAsync(Solicitud(tipo, (decimal)cantidad, producto: producto)));
        Assert.Empty(await _gestion.ConsultarHistorialAsync());
        Assert.All(await _gestion.ConsultarExistenciasAsync(), x => Assert.Equal(0m, x.StockActual));
    }

    [Fact]
    public async Task FormularioConStockDesactualizado_ImpideSobrescribirOtroMovimiento()
    {
        await _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.SaldoInicial, 10));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.AjusteConteo, 20, esperado: 0)));
        Assert.Single(await _gestion.ConsultarHistorialAsync());
        Assert.Equal(10m, (await _gestion.ConsultarExistenciasAsync()).Single(x => x.ProductoId == 1).StockActual);
    }

    [Fact]
    public async Task Desactivado_ConservaHistorialPeroNoAceptaMovimientos()
    {
        await _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.SaldoInicial, 3));
        await using (var contexto = new BodegaLuchitoDbContext(_opciones))
        {
            (await contexto.Productos.SingleAsync(x => x.Id == 1)).Activo = false;
            await contexto.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.Merma, 1, 3)));
        Assert.Single(await _gestion.ConsultarHistorialAsync());
    }

    [Fact]
    public async Task VendedoraOSesionCerrada_NoPuedeRegistrar()
    {
        _sesion.IniciarSesion(new UsuarioSesion { IdUsuario = 1, Rol = RolUsuario.Vendedora });
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.SaldoInicial, 1)));
        _sesion.CerrarSesion();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _gestion.ConsultarExistenciasAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AjusteSinMotivo_NoSeGuarda(string motivo)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _gestion.RegistrarAsync(Solicitud(
            TipoMovimientoInventario.AjusteConteo, 1, motivo: motivo)));
        Assert.Empty(await _gestion.ConsultarHistorialAsync());
    }

    [Fact]
    public async Task HistorialCompleto_NoSeLimitaYAbastecimientoSeFiltraAntesDelLimite()
    {
        await using (var contexto = new BodegaLuchitoDbContext(_opciones))
        {
            contexto.MovimientosInventario.Add(new MovimientoInventario {
                ProductoId = 1, UsuarioId = 1, Tipo = TipoMovimientoInventario.EntradaAbastecimiento,
                Cantidad = 1, StockAnterior = 0, StockPosterior = 1, FechaHora = DateTime.Now.AddDays(-1)
            });
            for (var i = 0; i < 205; i++)
                contexto.MovimientosInventario.Add(new MovimientoInventario {
                    ProductoId = 1, UsuarioId = 1, Tipo = TipoMovimientoInventario.AjusteConteo,
                    Cantidad = 1, StockAnterior = i + 1, StockPosterior = i + 2, FechaHora = DateTime.Now
                });
            await contexto.SaveChangesAsync();
        }
        Assert.Equal(206, (await _gestion.ConsultarHistorialAsync()).Count);
        var repositorio = new InventarioRepository(new TestDbContextFactory(_opciones));
        Assert.Single(await repositorio.ObtenerMovimientosAsync(tipo: TipoMovimientoInventario.EntradaAbastecimiento));
    }

    [Fact]
    public async Task CompraAnteriorSinMovimiento_ImpideSaldoInicialInclusoConStockCero()
    {
        await using (var contexto = new BodegaLuchitoDbContext(_opciones))
        {
            var proveedor = new BodegaLuchito.Domain.Proveedores.Entities.Proveedor {
                Nombre = "Proveedor", Ruc = "20123456789"
            };
            contexto.Abastecimientos.Add(new BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento {
                Proveedor = proveedor, Total = 10,
                Detalles = [new() { ProductoId = 1, Cantidad = 2, CostoUnitario = 5, TotalLinea = 10 }]
            });
            await contexto.SaveChangesAsync();
        }
        Assert.False((await _gestion.ConsultarExistenciasAsync()).Single(x => x.ProductoId == 1).PuedeRegistrarSaldoInicial);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.SaldoInicial, 5)));
        Assert.Empty(await _gestion.ConsultarHistorialAsync());
    }

    [Fact]
    public async Task ProductoNuevoConSaldo_SeGuardaJuntoConMovimientoSinCaja()
    {
        var datos = new BodegaLuchito.Application.Productos.DTOs.RegistrarProductoRequest(
            "Papa nueva", 1, "NUEVO-1", 3.50m, UnidadVenta.Peso, true, 0, 5);
        await _gestion.RegistrarAsync(new(0, TipoMovimientoInventario.SaldoInicial, 80.125m, 0, "Conteo inicial", datos));
        await using var contexto = new BodegaLuchitoDbContext(_opciones);
        var producto = await contexto.Productos.SingleAsync(x => x.CodigoBarras == "NUEVO-1");
        var movimiento = await contexto.MovimientosInventario.SingleAsync();
        Assert.Equal(80.125m, producto.StockActual);
        Assert.Equal(producto.Id, movimiento.ProductoId);
        Assert.Equal(0m, movimiento.StockAnterior);
        Assert.Equal(producto.StockActual, movimiento.StockPosterior);
        Assert.Empty(await contexto.Set<MovimientoCaja>().ToListAsync());
    }

    [Fact]
    public async Task ProductoNuevoConSaldoInvalido_NoDejaProductoVacio()
    {
        var datos = new BodegaLuchito.Application.Productos.DTOs.RegistrarProductoRequest(
            "Botella nueva", 1, null, 3m, UnidadVenta.Unidad, true, 0, 2);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _gestion.RegistrarAsync(new(0, TipoMovimientoInventario.SaldoInicial, 1.5m, 0, "Conteo inicial", datos)));
        await using var contexto = new BodegaLuchitoDbContext(_opciones);
        Assert.Equal(2, await contexto.Productos.CountAsync());
        Assert.Empty(await contexto.MovimientosInventario.ToListAsync());
    }

    [Fact]
    public async Task ProductoNuevoConCodigoRepetido_NoDuplicaProductoNiMovimiento()
    {
        var datos = new BodegaLuchito.Application.Productos.DTOs.RegistrarProductoRequest(
            "Producto nuevo", 1, "CODIGO-UNICO", 3m, UnidadVenta.Unidad, true, 0, 2);
        await _gestion.RegistrarAsync(new(0, TipoMovimientoInventario.SaldoInicial, 5, 0, "Conteo inicial", datos));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _gestion.RegistrarAsync(new(0, TipoMovimientoInventario.SaldoInicial, 5, 0, "Conteo inicial", datos)));
        await using var contexto = new BodegaLuchitoDbContext(_opciones);
        Assert.Equal(3, await contexto.Productos.CountAsync());
        Assert.Single(await contexto.MovimientosInventario.ToListAsync());
    }

    [Fact]
    public async Task StockPrevioSinMovimientos_NoEsCandidatoASaldoInicial()
    {
        await using (var contexto = new BodegaLuchitoDbContext(_opciones))
        {
            (await contexto.Productos.SingleAsync(x => x.Id == 1)).StockActual = 15;
            await contexto.SaveChangesAsync();
        }
        Assert.False((await _gestion.ConsultarExistenciasAsync()).Single(x => x.ProductoId == 1).PuedeRegistrarSaldoInicial);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _gestion.RegistrarAsync(Solicitud(TipoMovimientoInventario.SaldoInicial, 5, 15)));
    }
}
