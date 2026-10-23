using BodegaLuchito.Application.Abastecimiento.UseCases;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Infrastructure.Abastecimiento.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Compra = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Tests.Application.Abastecimiento;

public class FiltrosHistorialTests
{
    [Fact]
    public async Task FiltraAntesDelLimiteIncluyeTodoElDiaYProveedorInactivo()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
        await using (var db = new BodegaLuchitoDbContext(options))
        {
            await db.Database.EnsureCreatedAsync();
            var antiguo = new Proveedor { Nombre = "Proveedor antiguo", Ruc = "20123456789", Activo = false };
            var nuevo = new Proveedor { Nombre = "Proveedor actual", Ruc = "20987654321" };
            db.AddRange(antiguo, nuevo);
            await db.SaveChangesAsync();
            db.AddRange(Enumerable.Range(0, 205).Select(i => new Compra {
                ProveedorId = nuevo.Id, FechaHora = new DateTime(2026, 10, 20).AddMinutes(i),
                Total = 10, MetodoPago = MetodoPago.Yape }));
            db.AddRange(new Compra { ProveedorId = antiguo.Id, FechaHora = new DateTime(2026, 9, 30, 23, 59, 59), Total = 12, MetodoPago = MetodoPago.Efectivo },
                new Compra { ProveedorId = antiguo.Id, FechaHora = new DateTime(2026, 10, 1), Total = 13, MetodoPago = MetodoPago.Efectivo });
            await db.SaveChangesAsync();
        }
        var factory = new TestDbContextFactory(options);
        var caso = new ConsultarHistorialAbastecimientosUseCase(new AbastecimientoRepository(factory));
        var todos = await caso.ExecuteAsync();
        Assert.Equal(200, todos.Count);
        var septiembre = await caso.ExecuteAsync(desde: new DateTime(2026, 9, 30), hasta: new DateTime(2026, 9, 30), proveedorId: 1);
        Assert.Single(septiembre);
        Assert.Equal(12, septiembre[0].Total);
        Assert.Equal("Sin registro de responsable", septiembre[0].RegistradoPor);
        Assert.Equal("Proveedor antiguo", septiembre[0].Proveedor.Nombre);
        Assert.Empty(await caso.ExecuteAsync(desde: new DateTime(2026, 9, 30), hasta: new DateTime(2026, 9, 30), proveedorId: 2));
        await Assert.ThrowsAsync<ArgumentException>(() => caso.ExecuteAsync(desde: new DateTime(2026, 10, 1), hasta: new DateTime(2026, 9, 30)));
        await Assert.ThrowsAsync<ArgumentException>(() => caso.ExecuteAsync(hasta: DateTime.MaxValue));
        await Assert.ThrowsAsync<ArgumentException>(() => caso.ExecuteAsync(proveedorId: -1));
    }
}
