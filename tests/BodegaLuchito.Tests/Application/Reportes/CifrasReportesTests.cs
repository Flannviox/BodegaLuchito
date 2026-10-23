using BodegaLuchito.Application.BI.UseCases;
using BodegaLuchito.Application.Reportes.DTOs;
using BodegaLuchito.Application.Reportes.Servicios;
using BodegaLuchito.Application.Reportes.UseCases;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Infrastructure.Abastecimiento.Repositories;
using BodegaLuchito.Infrastructure.Caja.Repositories;
using BodegaLuchito.Infrastructure.Inventario.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Ventas.Repositories;
using BodegaLuchito.Tests.Helpers;
using ClosedXML.Excel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BodegaLuchito.Tests.Application.Reportes;

public sealed class CifrasReportesTests
{
    [Fact]
    public async Task VentasPorPeriodo_ExcluyeAnuladas_IncluyeExtremosYCargaProductoCategoria()
    {
        await using var datos = await Datos.CrearAsync();
        var ventas = await datos.Ventas.ObtenerVentasPorPeriodoAsync(DateTime.Today, DateTime.Today);
        Assert.Equal(2, ventas.Count);
        Assert.Equal(30m, ventas.Sum(v => v.Total));
        Assert.All(ventas, venta =>
        {
            Assert.False(venta.Anulado);
            Assert.Equal("Arroz", Assert.Single(venta.Detalles).Producto.Nombre);
            Assert.Equal("Abarrotes", venta.Detalles[0].Producto.Categoria.Nombre);
        });
        // Las anulaciones siguen disponibles en el historial operativo.
        Assert.Contains(await datos.Ventas.ObtenerHistorialVentasAsync(), v => v.Anulado);
    }

    [Fact]
    public async Task CierresPorPeriodo_UsaFechaCierre_YExcluyeSesionesAbiertas()
    {
        await using var datos = await Datos.CrearAsync();
        var cierres = await datos.Caja.ObtenerCierresPorPeriodoAsync(DateTime.Today, DateTime.Today);
        Assert.Equal(2, cierres.Count);
        Assert.Equal(-2m, cierres.Sum(c => c.DiferenciaEfectivo));
        Assert.Contains(cierres, c => c.FechaApertura.Date == DateTime.Today.AddDays(-1));
        Assert.All(cierres, c => Assert.Equal(EstadoSesionCaja.Cerrada, c.Estado));
    }

    [Fact]
    public async Task Dashboard_UsaVentasVigentes_EnKpisMediosGraficoYRanking_YCierresReales()
    {
        await using var datos = await Datos.CrearAsync();
        var dashboard = new ObtenerDatosDashboardUseCase(datos.Ventas,
            new InventarioRepository(datos.Factory), datos.Caja);
        var resultado = await dashboard.EjecutarAsync();
        Assert.Equal(30m, resultado.VentasDelDia.TotalMonto);
        Assert.Equal(2, resultado.VentasDelDia.CantidadTransacciones);
        Assert.Equal(30m, resultado.VentasDelMes.TotalMonto);
        Assert.Equal(DateTime.Today, resultado.FinPeriodo);
        Assert.Equal(DateTime.Today.Day, resultado.EvolucionVentasDelMes.Count);
        Assert.Equal(30m, resultado.VentasPorCategoria.Sum(c => c.TotalMonto));
        Assert.Equal(30m, resultado.MediosDePagoDelMes.Sum(m => m.TotalMonto));
        Assert.DoesNotContain(resultado.MediosDePagoDelMes, m => m.MedioPago == "Plin");
        Assert.Equal(7, resultado.EvolucionVentasUltimos7Dias.Count);
        Assert.Equal(30m, resultado.EvolucionVentasUltimos7Dias.Sum(d => d.TotalMonto));
        var producto = Assert.Single(resultado.ProductosMasVendidosDelMes);
        Assert.Equal("Arroz", producto.NombreProducto);
        Assert.Equal(30m, producto.MontoTotalGenerado);
        Assert.Equal(-2m, resultado.DiferenciaCajaDelDia);
    }

    [Theory]
    [InlineData("Ventas", 2)]
    [InlineData("Abastecimientos", 1)]
    [InlineData("Movimientos de Caja", 3)]
    public async Task Reporte_TodasLasTablasObtienenResumenCompletoDelMismoPeriodo(string tipo, int filas)
    {
        await using var datos = await Datos.CrearAsync();
        var reporte = await datos.Reportes.GenerarAsync(tipo, DateTime.Today, DateTime.Today);
        Assert.Equal(filas, reporte.Filas.Count);
        Assert.Equal(30m, reporte.TotalVentas);
        Assert.Equal(4m, reporte.TotalEgresos);
        Assert.Equal(-2m, reporte.DiferenciaCierres);
        Assert.Equal(2, reporte.CantidadCierres);
        if (tipo == "Movimientos de Caja") Assert.All(reporte.Filas.Cast<ReporteMovimientoCajaDto>(), m => Assert.Equal("Admin", m.Usuario));
        if (tipo == "Movimientos de Caja")
            Assert.Contains(reporte.Filas.Cast<ReporteMovimientoCajaDto>(), m => m.TipoMovimiento == "ReversionVenta");
    }

    [Fact]
    public async Task Reporte_PeriodoVacio_NoReutilizaCifrasAnteriores()
    {
        await using var datos = await Datos.CrearAsync();
        await datos.Reportes.GenerarAsync("Ventas", DateTime.Today, DateTime.Today);
        var reporte = await datos.Reportes.GenerarAsync("Ventas", DateTime.Today.AddYears(-1), DateTime.Today.AddYears(-1));
        Assert.Empty(reporte.Filas);
        Assert.Equal(0m, reporte.TotalVentas);
        Assert.Equal(0m, reporte.TotalEgresos);
        Assert.Equal(0m, reporte.DiferenciaCierres);
        Assert.Equal(0, reporte.CantidadCierres);
    }

    [Fact]
    public async Task Reportes_RechazanFechasInvertidasYTipoDesconocido()
    {
        var reportes = new ObtenerReportesUseCase(null!, null!, null!, null!);
        var inicio = DateTime.Today;
        var fin = inicio.AddDays(-1);
        await Assert.ThrowsAsync<ArgumentException>(() => reportes.GenerarAsync("Ventas", inicio, fin));
        await Assert.ThrowsAsync<ArgumentException>(() => reportes.ObtenerReporteVentasAsync(inicio, fin));
        await Assert.ThrowsAsync<ArgumentException>(() => reportes.ObtenerReporteAbastecimientosAsync(inicio, fin));
        await Assert.ThrowsAsync<ArgumentException>(() => reportes.ObtenerReporteCajaAsync(inicio, fin));
        await Assert.ThrowsAsync<ArgumentException>(() => reportes.GenerarAsync("Otro", inicio, inicio));
    }

    [Fact]
    public async Task Exportacion_ConservaPeriodoEImportesNumericosDelReporteGenerado()
    {
        await using var datos = await Datos.CrearAsync();
        var reporte = await datos.Reportes.GenerarAsync("Ventas", DateTime.Today, DateTime.Today);
        var carpeta = Path.Combine(Path.GetTempPath(), $"reportes-test-{Guid.NewGuid():N}");
        try
        {
            var ruta = new ExportacionExcelService().ExportarAExcel(reporte.Filas, reporte.Tipo,
                carpeta, reporte.Inicio, reporte.Fin);
            using var libro = new XLWorkbook(ruta);
            var hoja = libro.Worksheet("Ventas");
            Assert.Equal($"Período: {reporte.Inicio:dd/MM/yyyy} al {reporte.Fin:dd/MM/yyyy}", hoja.Cell(3, 1).GetString());
            Assert.Equal(XLDataType.Number, hoja.Cell(5, 8).DataType);
            Assert.Equal(30m, hoja.Cell(5, 8).GetValue<decimal>() + hoja.Cell(6, 8).GetValue<decimal>());
        }
        finally { if (Directory.Exists(carpeta)) Directory.Delete(carpeta, true); }
    }

    [Fact]
    public async Task Dashboard_MesHistorico_RespetaFebreroBisiestoYCategoriasSinAnuladas()
    {
        await using var datos = await Datos.CrearAsync();
        var caso = new ObtenerDatosDashboardUseCase(datos.Ventas, new InventarioRepository(datos.Factory), datos.Caja);
        var resultado = await caso.EjecutarAsync(new DateTime(2024, 2, 15));
        Assert.Equal(new DateTime(2024, 2, 1), resultado.InicioPeriodo);
        Assert.Equal(new DateTime(2024, 2, 29), resultado.FinPeriodo);
        Assert.Equal(70m, resultado.VentasDelMes.TotalMonto);
        Assert.Equal(70m, resultado.VentasDelDia.TotalMonto);
        Assert.Equal(29, resultado.EvolucionVentasDelMes.Count);
        Assert.Equal(70m, resultado.EvolucionVentasDelMes.Sum(v => v.TotalMonto));
        var categoria = Assert.Single(resultado.VentasPorCategoria);
        Assert.Equal("Abarrotes", categoria.Categoria);
        Assert.Equal(70m, categoria.TotalMonto);
        await Assert.ThrowsAsync<ArgumentException>(() => caso.EjecutarAsync(DateTime.Today.AddYears(1)));
    }
    private sealed class Datos : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly BodegaLuchitoDbContext _context;
        public TestDbContextFactory Factory { get; }
        public VentaRepository Ventas { get; }
        public CajaRepository Caja { get; }
        public ObtenerReportesUseCase Reportes { get; }

        private Datos(SqliteConnection connection, DbContextOptions<BodegaLuchitoDbContext> options)
        {
            _connection = connection;
            _context = new BodegaLuchitoDbContext(options);
            Factory = new TestDbContextFactory(options);
            Ventas = new VentaRepository(Factory);
            Caja = new CajaRepository(_context);
            Reportes = new ObtenerReportesUseCase(Ventas, new AbastecimientoRepository(Factory), Caja, new BodegaLuchito.Infrastructure.Autenticacion.Repositories.UsuarioRepository(Factory));
        }

        public static async Task<Datos> CrearAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
            var datos = new Datos(connection, options);
            var db = datos._context;
            await db.Database.EnsureCreatedAsync();
            var hoy = DateTime.Today;
            var usuario = new Usuario { NombreUsuario = "admin-test", NombreCompleto = "Admin", PasswordHash = "hash", Rol = RolUsuario.Administradora, Activo = true };
            var producto = new Producto { Nombre = "Arroz", Categoria = new Categoria { Nombre = "Abarrotes" }, UnidadVenta = UnidadVenta.Unidad, ControlaInventario = true, Activo = true, PrecioVenta = 10m, StockActual = 20m };
            db.AddRange(usuario, producto);
            await db.SaveChangesAsync();
            Venta CrearVenta(DateTime fecha, decimal total, MetodoPago pago, bool anulada = false) => new()
            {
                UsuarioId = usuario.Id, FechaHora = fecha, Total = total, Subtotal = total, MetodoPago = pago, Anulado = anulada,
                Detalles = new() { new() { ProductoId = producto.Id, Cantidad = 1, PrecioUnitario = total, Subtotal = total } }
            };
            db.AddRange(CrearVenta(hoy, 10m, MetodoPago.Efectivo),
                CrearVenta(hoy.AddDays(1).AddTicks(-1), 20m, MetodoPago.Yape),
                CrearVenta(hoy.AddHours(12), 50m, MetodoPago.Plin, true),
                CrearVenta(hoy.AddDays(1), 100m, MetodoPago.Plin),
                CrearVenta(new DateTime(2024, 2, 29), 70m, MetodoPago.Efectivo),
                CrearVenta(new DateTime(2024, 2, 28), 90m, MetodoPago.Plin, true),
                CrearVenta(new DateTime(2024, 3, 1), 900m, MetodoPago.Yape));
            var cierre = new SesionCaja { UsuarioAperturaId = usuario.Id, FechaApertura = hoy.AddDays(-1), FechaCierre = hoy, Estado = EstadoSesionCaja.Cerrada, DiferenciaEfectivo = -3m };
            db.AddRange(cierre,
                new SesionCaja { UsuarioAperturaId = usuario.Id, FechaApertura = hoy, FechaCierre = hoy.AddDays(1).AddTicks(-1), Estado = EstadoSesionCaja.Cerrada, DiferenciaEfectivo = 1m },
                new SesionCaja { UsuarioAperturaId = usuario.Id, FechaApertura = hoy, FechaCierre = hoy.AddDays(1), Estado = EstadoSesionCaja.Cerrada, DiferenciaEfectivo = 8m },
                new SesionCaja { UsuarioAperturaId = usuario.Id, FechaApertura = hoy, Estado = EstadoSesionCaja.Abierta });
            db.Add(new BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento
            {
                Proveedor = new Proveedor { Nombre = "Proveedor", Ruc = "20123456789" }, FechaHora = hoy,
                Total = 4m, MetodoPago = MetodoPago.Efectivo
            });
            await db.SaveChangesAsync();
            foreach (var tipo in new[] { TipoMovimientoCaja.IngresoVenta, TipoMovimientoCaja.ReversionVenta, TipoMovimientoCaja.EgresoAbastecimiento })
                db.Add(new MovimientoCaja { SesionCajaId = cierre.Id, UsuarioId = usuario.Id, FechaHora = hoy, Tipo = tipo, MetodoPago = MetodoPago.Efectivo, Monto = 4m });
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            return datos;
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
