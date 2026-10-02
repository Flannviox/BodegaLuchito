using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Ventas.DTOs;
using BodegaLuchito.Application.Ventas.UseCases;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Ventas.Repositories;
using BodegaLuchito.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BodegaLuchito.Tests.Application.Ventas;

public sealed class RegistrarVentaUseCaseTests
{
    private static async Task<(SqliteConnection connection, DbContextOptions<BodegaLuchitoDbContext> options)>
        CrearBaseEnMemoriaAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new BodegaLuchitoDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return (connection, options);
    }

    private static async Task<(int usuarioId, int productoId)> SembrarDatosPruebaAsync(DbContextOptions<BodegaLuchitoDbContext> options)
    {
        await using var context = new BodegaLuchitoDbContext(options);

        var usuario = new Usuario
        {
            NombreCompleto = "Vendedora Prueba",
            NombreUsuario = "vendedora",
            PasswordHash = "hash-prueba",
            Rol = RolUsuario.Vendedora,
            Activo = true
        };

        var categoria = new Categoria { Nombre = "Abarrotes" };

        var producto = new Producto
        {
            Nombre = "Galletas Prueba",
            Categoria = categoria,
            PrecioVenta = 5m,
            UnidadVenta = UnidadVenta.Unidad,
            ControlaInventario = true,
            StockActual = 100m,
            StockMinimo = 10m,
            Activo = true
        };

        context.Set<Usuario>().Add(usuario);
        context.Set<Categoria>().Add(categoria);
        context.Set<Producto>().Add(producto);

        await context.SaveChangesAsync();

        return (usuario.Id, producto.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ConDatosValidos_RegistraVentaYDetalles()
    {
        var (connection, options) = await CrearBaseEnMemoriaAsync();
        await using var connectionDispose = connection;

        // Obtenemos los IDs reales generados en la BD en memoria
        var (usuarioId, productoId) = await SembrarDatosPruebaAsync(options);

        var factory = new TestDbContextFactory(options);
        var repository = new VentaRepository(factory);
        var useCase = new RegistrarVentaUseCase(repository);

        var request = new RegistrarVentaRequest
        {
            UsuarioId = usuarioId,
            Subtotal = 10m,
            IGV = 1.8m,
            Total = 11.8m,
            MetodoPago = MetodoPago.Yape,
            Detalles =
            [
                new DetalleVentaRequest { ProductoId = productoId, Cantidad = 2, PrecioUnitario = 5m, Subtotal = 10m }
            ]
        };

        // Ejecución
        int nuevaVentaId = await useCase.ExecuteAsync(request);

        // Verificación
        await using var context = new BodegaLuchitoDbContext(options);
        var ventaGuardada = await context.Set<Venta>().Include(v => v.Detalles).SingleAsync();

        Assert.True(nuevaVentaId > 0);
        Assert.Equal(MetodoPago.Yape, ventaGuardada.MetodoPago);
        Assert.Equal(11.8m, ventaGuardada.Total);
        Assert.Single(ventaGuardada.Detalles);
        Assert.False(ventaGuardada.Anulado);
    }

    [Fact]
    public async Task ExecuteAsync_CarritoVacio_LanzaArgumentException()
    {
        var (connection, options) = await CrearBaseEnMemoriaAsync();
        await using var connectionDispose = connection;

        var factory = new TestDbContextFactory(options);
        var useCase = new RegistrarVentaUseCase(new VentaRepository(factory));

        var request = new RegistrarVentaRequest
        {
            UsuarioId = 1,
            Total = 10m,
            MetodoPago = MetodoPago.Efectivo,
            Detalles = []
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(request));
        Assert.Contains("al menos un producto", ex.Message);
    }
}
