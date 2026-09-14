using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.UseCases;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.UseCases;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Application.Proveedores.DTOs;
using BodegaLuchito.Application.Proveedores.UseCases;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Infrastructure.Autenticacion.Repositories;
using BodegaLuchito.Infrastructure.Autenticacion.Security;
using BodegaLuchito.Infrastructure.Caja.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Productos.Repositories;
using BodegaLuchito.Infrastructure.Proveedores.Repositories;
using BodegaLuchito.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BodegaLuchito.Tests.Infraestructure.Persistence;

public sealed class RecorridoIntegradoTests
{
    [Fact]
    public async Task MigracionNuevaConservaProductosExistentes()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"bodega-migracion-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite($"Data Source={Path.Combine(directory, "bodega.db")}")
                .Options;

            await using (var context = new BodegaLuchitoDbContext(options))
            {
                await context.GetService<IMigrator>().MigrateAsync("20260913000055_AddAutenticacion");
                context.Set<Producto>().Add(new Producto
                {
                    Nombre = "Producto previo",
                    Categoria = "Abarrotes",
                    PrecioVenta = 3m,
                    UnidadVenta = UnidadVenta.Unidad,
                    Activo = true
                });
                await context.SaveChangesAsync();
            }

            await using (var context = new BodegaLuchitoDbContext(options))
            {
                await context.Database.MigrateAsync();
                Assert.Empty(await context.Database.GetPendingMigrationsAsync());
                Assert.Equal("Producto previo", (await context.Set<Producto>().SingleAsync()).Nombre);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task LosDatosPersistenTrasReabrirLaBaseMigrada()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"bodega-integracion-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);

        try
        {
            var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite($"Data Source={Path.Combine(directory, "bodega.db")}")
                .Options;
            var factory = new TestDbContextFactory(options);
            var usuarioRepository = new UsuarioRepository(factory);
            var hasher = new Pbkdf2PasswordHasher();

            await using (var context = new BodegaLuchitoDbContext(options))
            {
                await context.Database.MigrateAsync();
                Assert.Empty(await context.Database.GetPendingMigrationsAsync());
            }

            var crearAdministrador = new CrearAdministradorInicialUseCase(usuarioRepository, hasher);
            var error = await crearAdministrador.EjecutarAsync(new CrearAdministradorInicialRequest
            {
                NombreCompleto = "Administradora Prueba",
                NombreUsuario = "admin",
                Password = "ClaveSegura123",
                ConfirmarPassword = "ClaveSegura123"
            });
            Assert.Null(error);

            var sesion = new SesionUsuario();
            var ingreso = await new IniciarSesionUseCase(usuarioRepository, hasher, sesion)
                .EjecutarAsync(new IniciarSesionRequest { NombreUsuario = "admin", Password = "ClaveSegura123" });
            Assert.True(ingreso.Exitoso);
            var usuarioId = sesion.UsuarioActual!.IdUsuario;

            await new RegistrarProductoUseCase(new ProductoRepository(factory))
                .EjecutarAsync(new RegistrarProductoRequest("Arroz", "Abarrotes", "123456", 5m,
                    UnidadVenta.Unidad, true, 10m, 2m));
            await new RegistrarProveedorUseCase(new ProveedorRepository(factory))
                .ExecuteAsync(new RegistrarProveedorRequest { Nombre = "Proveedor prueba", Ruc = "12345678901" });

            await using (var context = new BodegaLuchitoDbContext(options))
            {
                await new AbrirCajaUseCase(new CajaRepository(context))
                    .EjecutarAsync(new AbrirCajaRequest(usuarioId, 100m));
            }

            await using (var context = new BodegaLuchitoDbContext(options))
            {
                Assert.Single(await context.Set<Producto>().ToListAsync());
                Assert.Single(await context.Set<Proveedor>().ToListAsync());
                Assert.Equal(100m, (await context.Set<SesionCaja>().SingleAsync()).FondoInicial);

                await new CerrarCajaUseCase(new CajaRepository(context))
                    .EjecutarAsync(new CerrarCajaRequest(usuarioId, 100m, 0m, 0m, null));
            }

            await using (var context = new BodegaLuchitoDbContext(options))
            {
                Assert.Equal(EstadoSesionCaja.Cerrada, (await context.Set<SesionCaja>().SingleAsync()).Estado);
                Assert.Equal("Arroz", (await context.Set<Producto>().SingleAsync()).Nombre);
                Assert.Equal("Proveedor prueba", (await context.Set<Proveedor>().SingleAsync()).Nombre);
            }
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            Directory.Delete(directory, recursive: true);
        }
    }
}
