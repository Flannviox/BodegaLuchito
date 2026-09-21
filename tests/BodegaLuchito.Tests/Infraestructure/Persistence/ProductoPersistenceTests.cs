using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

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

            // Insertar una categoría de prueba para evitar un error de llave foránea (FK)
            arrangeContext.Categorias.Add(new Categoria { Id = 1, Nombre = "Bebidas" });
            await arrangeContext.SaveChangesAsync();
        }

        var producto = new Producto
        {
            Nombre = "Coca Cola 500 ml",
            CategoriaId = 1, // Corregido a CategoriaId (int)
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
            1, // Corregido: se verifica el ID (int)
            productoGuardado.CategoriaId);

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

        // Insertamos categoría falsa para la relación
        context.Categorias.Add(new Categoria { Id = 1, Nombre = "Prueba" });
        await context.SaveChangesAsync();

        var productoUno = new Producto
        {
            Nombre = "Producto A",
            CategoriaId = 1, // Corregido
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
            CategoriaId = 1, // Corregido
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
