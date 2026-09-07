using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Tests.Infrastructure.Persistence;

public sealed class ProductoPersistenceTests
{
    [Fact]
    public async Task Producto_DebeGuardarseYConsultarseCorrectamente()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var arrangeContext =
                     new BodegaLuchitoDbContext(options))
        {
            await arrangeContext.Database.EnsureCreatedAsync();
        }

        var producto = new Producto
        {
            Nombre = "Coca Cola 500 ml",
            Categoria = "Bebidas",
            CodigoBarras = "7751234567890",
            PrecioVenta = 3.50m,
            UnidadVenta = UnidadVenta.Unidad,
            ControlaInventario = true,
            StockActual = 20,
            StockMinimo = 5,
            Activo = true
        };

        // Act - Guardar
        await using (var writeContext =
                     new BodegaLuchitoDbContext(options))
        {
            writeContext.Productos.Add(producto);

            await writeContext.SaveChangesAsync();
        }

        // Act - Consultar utilizando otro DbContext
        await using var readContext =
            new BodegaLuchitoDbContext(options);

        var productoGuardado =
            await readContext.Productos
                .AsNoTracking()
                .SingleAsync(x =>
                    x.CodigoBarras == "7751234567890");

        // Assert
        Assert.NotEqual(
            0,
            productoGuardado.Id);

        Assert.Equal(
            "Coca Cola 500 ml",
            productoGuardado.Nombre);

        Assert.Equal(
            "Bebidas",
            productoGuardado.Categoria);

        Assert.Equal(
            "7751234567890",
            productoGuardado.CodigoBarras);

        Assert.Equal(
            3.50m,
            productoGuardado.PrecioVenta);

        Assert.Equal(
            UnidadVenta.Unidad,
            productoGuardado.UnidadVenta);

        Assert.True(
            productoGuardado.ControlaInventario);

        Assert.Equal(
            20m,
            productoGuardado.StockActual);

        Assert.Equal(
            5m,
            productoGuardado.StockMinimo);

        Assert.True(
            productoGuardado.Activo);
    }
    [Fact]
    public async Task CodigoBarrasDuplicado_DebeFallar()
    {
        // Arrange
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var context =
            new BodegaLuchitoDbContext(options);

        await context.Database.EnsureCreatedAsync();

        var productoUno = new Producto
        {
            Nombre = "Producto A",
            Categoria = "Prueba",
            CodigoBarras = "123456789",
            PrecioVenta = 2m,
            UnidadVenta = UnidadVenta.Unidad,
            ControlaInventario = true,
            StockActual = 10,
            StockMinimo = 2
        };

        var productoDos = new Producto
        {
            Nombre = "Producto B",
            Categoria = "Prueba",
            CodigoBarras = "123456789",
            PrecioVenta = 3m,
            UnidadVenta = UnidadVenta.Unidad,
            ControlaInventario = true,
            StockActual = 10,
            StockMinimo = 2
        };

        context.Productos.Add(productoUno);

        await context.SaveChangesAsync();

        context.Productos.Add(productoDos);

        // Act
        var action = async () =>
            await context.SaveChangesAsync();

        // Assert
        await Assert.ThrowsAsync<DbUpdateException>(action);
    }
}
