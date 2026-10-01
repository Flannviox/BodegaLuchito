using System;
using System.Threading.Tasks;
using BodegaLuchito.Application.Common.Session;
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
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
        await using (var context = new BodegaLuchitoDbContext(options)) { await context.Database.EnsureCreatedAsync(); }

        var factory = new TestDbContextFactory(options);
        var repository = new ProductoRepository(factory);
        var useCase = new ModificarProductoUseCase(repository, new SesionUsuario());

        var request = new ModificarProductoRequest(999, "Inexistente", 1, null, 10m, UnidadVenta.Unidad, false, 0, 0);

        var action = async () => await useCase.EjecutarAsync(request);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Contains("no existe", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_CodigoBarrasDuplicado_LanzaInvalidOperationException()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            // FIX: Agregar categoría
            context.Categorias.Add(new Categoria { Id = 1, Nombre = "Cat" });

            context.Productos.Add(new Producto { Id = 1, Nombre = "Prod 1", CategoriaId = 1, CodigoBarras = "111", PrecioVenta = 5 });
            context.Productos.Add(new Producto { Id = 2, Nombre = "Prod 2", CategoriaId = 1, CodigoBarras = "222", PrecioVenta = 5 });
            await context.SaveChangesAsync();
        }

        var factory = new TestDbContextFactory(options);
        var repository = new ProductoRepository(factory);
        var useCase = new ModificarProductoUseCase(repository, new SesionUsuario());

        var request = new ModificarProductoRequest(1, "Prod 1 Mod", 1, "222", 10m, UnidadVenta.Unidad, false, 0, 0);

        var action = async () => await useCase.EjecutarAsync(request);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(action);
        Assert.Contains("ya está registrado en otro producto", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_DatosValidos_ActualizaCorrectamente()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;
        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            // FIX: Agregar las 2 categorías que usaremos en la prueba
            context.Categorias.Add(new Categoria { Id = 1, Nombre = "Vieja" });
            context.Categorias.Add(new Categoria { Id = 2, Nombre = "Nueva" });

            context.Productos.Add(new Producto
            {
                Id = 1,
                Nombre = "Viejo",
                CategoriaId = 1,
                PrecioVenta = 5,
                StockActual = 10m,
                Activo = true
            });
            await context.SaveChangesAsync();
        }

        var factory = new TestDbContextFactory(options);
        var repository = new ProductoRepository(factory);
        var sesionUsuario = new SesionUsuario();
        sesionUsuario.IniciarSesion(new UsuarioSesion
        {
            IdUsuario = 1,
            NombreCompleto = "Rocio Falcon"
        });
        var useCase = new ModificarProductoUseCase(repository, sesionUsuario);

        var request = new ModificarProductoRequest(1, "Nuevo", 2, "555", 10m, UnidadVenta.Peso, true, 50m, 10m);

        await useCase.EjecutarAsync(request);

        await using var readContext = new BodegaLuchitoDbContext(options);
        var actualizado = await readContext.Productos.SingleAsync(p => p.Id == 1);
        Assert.Equal("Nuevo", actualizado.Nombre);
        Assert.Equal(2, actualizado.CategoriaId);
        Assert.Equal("555", actualizado.CodigoBarras);
        Assert.Equal(10m, actualizado.PrecioVenta);
        Assert.Equal(10m, actualizado.StockActual);

        var historial = await readContext.HistorialPreciosProducto.SingleAsync();
        Assert.Equal(5m, historial.PrecioAnterior);
        Assert.Equal(10m, historial.PrecioNuevo);
        Assert.Equal("Rocio Falcon", historial.UsuarioNombre);
    }

    [Fact]
    public async Task EjecutarAsync_SinCambioDePrecio_NoRegistraHistorial()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>().UseSqlite(connection).Options;

        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Categorias.Add(new Categoria { Id = 1, Nombre = "Bebidas" });
            context.Productos.Add(new Producto
            {
                Id = 1,
                Nombre = "Agua",
                CategoriaId = 1,
                PrecioVenta = 3m,
                Activo = true
            });
            await context.SaveChangesAsync();
        }

        var repository = new ProductoRepository(new TestDbContextFactory(options));
        var useCase = new ModificarProductoUseCase(repository, new SesionUsuario());
        var request = new ModificarProductoRequest(
            1,
            "Agua sin gas",
            1,
            null,
            3m,
            UnidadVenta.Unidad,
            true,
            99m,
            2m);

        await useCase.EjecutarAsync(request);

        await using var readContext = new BodegaLuchitoDbContext(options);
        Assert.Equal(0, await readContext.HistorialPreciosProducto.CountAsync());
    }
}
