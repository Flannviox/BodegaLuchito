using System;
using System.IO;
using System.Threading.Tasks;
using BodegaLuchito.Domain.Abastecimiento.Entities;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Tests.Infraestructure.Persistence;

public sealed class AbastecimientoOperativoMigrationTests
{
    [Fact]
    public async Task MigracionOperativa_ConservaCostosDeAbastecimientosExistentes()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"bodega-abastecimiento-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite($"Data Source={Path.Combine(directory, "bodega.db")}")
                .Options;

            await using (var context = new BodegaLuchitoDbContext(options))
            {
                var migrator = context.GetService<IMigrator>();
                await migrator.MigrateAsync("20260922022209_AgregarAbastecimientos");

                var categoria = new Categoria { Nombre = "Bebidas" };
                var proveedor = new Proveedor { Nombre = "Proveedor previo", Ruc = "12345678901" };
                context.AddRange(categoria, proveedor);
                await context.SaveChangesAsync();

                var producto = new Producto
                {
                    Nombre = "Gaseosa previa",
                    CategoriaId = categoria.Id,
                    PrecioVenta = 3m,
                    UnidadVenta = UnidadVenta.Unidad,
                    ControlaInventario = true,
                    StockActual = 5m,
                    StockMinimo = 1m,
                    Activo = true
                };
                context.Add(producto);
                await context.SaveChangesAsync();

                context.Add(new EntidadAbastecimiento
                {
                    ProveedorId = proveedor.Id,
                    FechaHora = DateTime.Now,
                    MetodoPago = MetodoPago.Efectivo,
                    Total = 25m,
                    Detalles =
                    {
                        new DetalleAbastecimiento
                        {
                            ProductoId = producto.Id,
                            Cantidad = 10m,
                            CostoUnitario = 2.5m,
                            TotalLinea = 25m
                        }
                    }
                });
                await context.SaveChangesAsync();
            }

            await using (var context = new BodegaLuchitoDbContext(options))
            {
                await context.Database.MigrateAsync();

                var detalle = await context.Set<DetalleAbastecimiento>().SingleAsync();
                Assert.Equal(2.5m, detalle.CostoUnitario);
                Assert.Equal(25m, detalle.TotalLinea);
                Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }
}
