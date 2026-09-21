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
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            // FIX: Agregar la categoría primero para cumplir con la Llave Foránea
            context.Categorias.Add(new Categoria { Id = 1, Nombre = "Cat" });

            context.Productos.Add(new Producto { Id = 1, Nombre = "Prod 1", CategoriaId = 1, PrecioVenta = 5, Activo = true });
            await context.SaveChangesAsync();
        }

        var factory = new TestDbContextFactory(options);
        var repository = new ProductoRepository(factory);
        var useCase = new EliminarProductoUseCase(repository);

        await useCase.EjecutarAsync(1);

        await using var readContext = new BodegaLuchitoDbContext(options);
        var productoDb = await readContext.Productos.SingleAsync(p => p.Id == 1);

        Assert.False(productoDb.Activo);
    }
}
