using System;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Productos.Repositories;
using BodegaLuchito.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BodegaLuchito.Tests.Application.Productos;

public sealed class EliminarProductoUseCaseTests
{
    [Fact]
    public async Task EjecutarAsync_ProductoExistente_DesactivaProducto()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Productos.Add(new Producto { Id = 1, Nombre = "Prod 1", Categoria = "Cat", PrecioVenta = 5, Activo = true });
            await context.SaveChangesAsync();
        }

        var factory = new TestDbContextFactory(options);
        var repository = new ProductoRepository(factory);
        var useCase = new EliminarProductoUseCase(repository);

        // Act
        await useCase.EjecutarAsync(1);

        // Assert
        await using var readContext = new BodegaLuchitoDbContext(options);
        var productoDb = await readContext.Productos.SingleAsync(p => p.Id == 1);

        // Verificamos que se haya hecho la eliminación lógica
        Assert.False(productoDb.Activo);
    }
}
