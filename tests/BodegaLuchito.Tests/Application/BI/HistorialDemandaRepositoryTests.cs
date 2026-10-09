using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Infrastructure.BI;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Tests.Application.BI;

public sealed class HistorialDemandaRepositoryTests
{
    [Fact]
    public async Task ConsultaAgrupaCantidades_ExcluyeAnuladasHoyEInactivos_SinEscribir()
    {
        var ruta = Path.Combine(Path.GetTempPath(), $"prediccion-{Guid.NewGuid():N}.db");
        var opciones = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite($"Data Source={ruta};Pooling=False").Options;
        try
        {
            await using (var db = new BodegaLuchitoDbContext(opciones))
            {
                await db.Database.MigrateAsync();
                db.Add(new Usuario { Id = 1, NombreUsuario = "prueba", NombreCompleto = "Prueba", PasswordHash = "prueba" });
                db.Categorias.Add(new Categoria { Id = 1, Nombre = "Verduras" });
                db.Productos.AddRange(Enumerable.Range(1, 3).Select(i => new Producto
                {
                    Id = i, Nombre = "Papa " + i, CategoriaId = 1, UnidadVenta = UnidadVenta.Peso,
                    StockActual = 80, ControlaInventario = i != 3, Activo = i != 2, FechaCreacion = DateTime.Today.AddDays(-100)
                }));
                db.Ventas.AddRange(Venta(-4, false, 1, 1.125m), Venta(-4, false, 1, 2.250m),
                    Venta(-3, true, 1, 999), Venta(0, false, 1, 888), Venta(-2, false, 2, 777), Venta(-2, false, 3, 666));
                await db.SaveChangesAsync();
            }
            var repo = new HistorialDemandaRepository(new TestDbContextFactory(opciones));
            var fila = Assert.Single(await repo.ConsultarAsync(DateTime.Today.AddDays(-90), DateTime.Today));
            Assert.Equal("kg", fila.Unidad);
            Assert.Equal(80, fila.Stock);
            Assert.Equal(DateTime.Today.AddDays(-4), fila.Desde);
            Assert.Equal(3.375m, Assert.Single(fila.Ventas).Cantidad);
            await using var comprobar = new BodegaLuchitoDbContext(opciones);
            Assert.Equal(6, await comprobar.Ventas.CountAsync());
            Assert.All(await comprobar.Productos.ToListAsync(), p => Assert.Equal(80, p.StockActual));
            Assert.Empty(await comprobar.MovimientosInventario.ToListAsync());
            Assert.False(comprobar.Database.HasPendingModelChanges());
        }
        finally { if (File.Exists(ruta)) File.Delete(ruta); }
    }

    private static Venta Venta(int dias, bool anulado, int producto, decimal cantidad) => new()
    {
        UsuarioId = 1, FechaHora = DateTime.Today.AddDays(dias).AddHours(10), Anulado = anulado,
        Total = cantidad * 2, Subtotal = cantidad * 2,
        Detalles = [new DetalleVenta { ProductoId = producto, Cantidad = cantidad, PrecioUnitario = 2, Subtotal = cantidad * 2 }]
    };
}
