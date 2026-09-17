using System;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.DTOs;
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

public sealed class ModificarProductoUseCaseTests
{
    [Fact]
    public async Task EjecutarAsync_ProductoInexistente_LanzaInvalidOperationException()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
        await using (var context = new BodegaLuchitoDbContext(options)) { await context.Database.EnsureCreatedAsync(); }

        var factory = new TestDbContextFactory(options);
        var repository = new ProductoRepository(factory);
        var useCase = new ModificarProductoUseCase(repository);

        var request = new ModificarProductoRequest(999, "Inexistente", "Test", null, 10m, UnidadVenta.Unidad, false, 0, 0);

        // Act
        var action = async () => await useCase.EjecutarAsync(request);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Contains("no existe", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_CodigoBarrasDuplicado_LanzaInvalidOperationException()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Productos.Add(new Producto { Id = 1, Nombre = "Prod 1", Categoria = "Cat", CodigoBarras = "111", PrecioVenta = 5 });
            context.Productos.Add(new Producto { Id = 2, Nombre = "Prod 2", Categoria = "Cat", CodigoBarras = "222", PrecioVenta = 5 });
            await context.SaveChangesAsync();
        }

        var factory = new TestDbContextFactory(options);
        var repository = new ProductoRepository(factory);
        var useCase = new ModificarProductoUseCase(repository);

        // Intentar ponerle el código "222" (que es del producto 2) al producto 1
        var request = new ModificarProductoRequest(1, "Prod 1 Mod", "Cat", "222", 10m, UnidadVenta.Unidad, false, 0, 0);

        // Act
        var action = async () => await useCase.EjecutarAsync(request);

        // Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Contains("ya está registrado en otro producto", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_DatosValidos_ActualizaCorrectamente()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Productos.Add(new Producto { Id = 1, Nombre = "Viejo", Categoria = "Vieja", PrecioVenta = 5, Activo = true });
            await context.SaveChangesAsync();
        }

        var factory = new TestDbContextFactory(options);
        var repository = new ProductoRepository(factory);
        var useCase = new ModificarProductoUseCase(repository);

        var request = new ModificarProductoRequest(1, "Nuevo", "Nueva Cat", "555", 10m, UnidadVenta.Peso, true, 50m, 10m);

        // Act
        await useCase.EjecutarAsync(request);

        // Assert
        await using var readContext = new BodegaLuchitoDbContext(options);
        var actualizado = await readContext.Productos.SingleAsync(p => p.Id == 1);
        Assert.Equal("Nuevo", actualizado.Nombre);
        Assert.Equal("Nueva Cat", actualizado.Categoria);
        Assert.Equal("555", actualizado.CodigoBarras);
        Assert.Equal(10m, actualizado.PrecioVenta);
        Assert.Equal(50m, actualizado.StockActual);
    }
}
