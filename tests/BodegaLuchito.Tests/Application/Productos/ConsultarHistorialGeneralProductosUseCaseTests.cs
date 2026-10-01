using System;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Productos.Repositories;
using BodegaLuchito.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BodegaLuchito.Tests.Application.Productos;

public sealed class ConsultarHistorialGeneralProductosUseCaseTests
{
    [Fact]
    public async Task EjecutarAsync_ReuneCambiosDePrecioYEstadoConElNombreDelProducto()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Categorias.Add(new Categoria { Id = 1, Nombre = "Bebidas" });
            context.Productos.Add(new Producto
            {
                Id = 1,
                Nombre = "Agua mineral",
                CategoriaId = 1,
                PrecioVenta = 3.50m,
                Activo = false
            });
            context.HistorialPreciosProducto.Add(new HistorialPrecioProducto
            {
                ProductoId = 1,
                PrecioAnterior = 3m,
                PrecioNuevo = 3.50m,
                FechaHora = new DateTime(2026, 10, 1, 10, 0, 0),
                UsuarioNombre = "Rocío"
            });
            context.HistorialActividadProducto.Add(new HistorialActividadProducto
            {
                ProductoId = 1,
                Tipo = TipoActividadProducto.Desactivacion,
                Descripcion = "Producto desactivado del catálogo.",
                FechaHora = new DateTime(2026, 10, 1, 11, 0, 0),
                UsuarioNombre = "Rocío"
            });
            await context.SaveChangesAsync();
        }

        var useCase = new ConsultarHistorialGeneralProductosUseCase(
            new ProductoRepository(new TestDbContextFactory(options)));

        var historial = await useCase.EjecutarAsync();

        Assert.Collection(
            historial,
            cambio =>
            {
                Assert.Equal("Agua mineral", cambio.Producto);
                Assert.Equal("Producto desactivado", cambio.Accion);
            },
            cambio =>
            {
                Assert.Equal("Agua mineral", cambio.Producto);
                Assert.Equal("Precio actualizado", cambio.Accion);
                Assert.Contains("Precio de venta", cambio.Detalle);
            });
    }
}
