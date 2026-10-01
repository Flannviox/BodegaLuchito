using System.Threading.Tasks;
using BodegaLuchito.Application.Common.Session;
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

public sealed class ReactivarProductoUseCaseTests
{
    [Fact]
    public async Task EjecutarAsync_ProductoInactivo_LoReactivaYRegistraLaActividad()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite(connection)
            .Options;

        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            context.Categorias.Add(new Categoria { Id = 1, Nombre = "Abarrotes" });
            context.Productos.Add(new Producto
            {
                Id = 1,
                Nombre = "Arroz",
                CategoriaId = 1,
                PrecioVenta = 4,
                Activo = false
            });
            await context.SaveChangesAsync();
        }

        var sesion = new SesionUsuario();
        sesion.IniciarSesion(new UsuarioSesion
        {
            IdUsuario = 1,
            NombreCompleto = "Rocio Falcon"
        });
        var useCase = new ReactivarProductoUseCase(
            new ProductoRepository(new TestDbContextFactory(options)),
            sesion);

        await useCase.EjecutarAsync(1);

        await using var readContext = new BodegaLuchitoDbContext(options);
        var producto = await readContext.Productos.SingleAsync();
        var actividad = await readContext.HistorialActividadProducto.SingleAsync();
        Assert.True(producto.Activo);
        Assert.Equal(TipoActividadProducto.Reactivacion, actividad.Tipo);
        Assert.Equal("Rocio Falcon", actividad.UsuarioNombre);
    }
}
