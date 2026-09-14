using BodegaLuchito.Application.Autenticacion.DTOs;
using BodegaLuchito.Application.Autenticacion.UseCases;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Autenticacion.Enums;
using BodegaLuchito.Infrastructure.Autenticacion.Repositories;
using BodegaLuchito.Infrastructure.Autenticacion.Security;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Tests.Helpers;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Tests.Application.Autenticacion;

public sealed class GestionUsuariosUseCaseTests
{
    [Fact]
    public async Task Vendedora_NoDebePoderRegistrarUsuarios()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var context =
                     new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
        }

        var factory =
            new TestDbContextFactory(options);

        var repository =
            new UsuarioRepository(factory);

        var hasher =
            new Pbkdf2PasswordHasher();

        var sesion =
            new SesionUsuario();

        sesion.IniciarSesion(
            new UsuarioSesion
            {
                IdUsuario = 1,
                NombreCompleto = "Maria Perez",
                NombreUsuario = "maria",
                Rol = RolUsuario.Vendedora
            });

        var useCase =
            new RegistrarUsuarioUseCase(
                repository,
                hasher,
                sesion);

        var error =
            await useCase.EjecutarAsync(
                new RegistrarUsuarioRequest
                {
                    NombreCompleto = "Otro Usuario",
                    NombreUsuario = "otro",
                    Password = "ClaveSegura123",
                    ConfirmarPassword = "ClaveSegura123",
                    Rol = RolUsuario.Vendedora
                });

        Assert.Equal(
            "No tiene permisos para registrar usuarios.",
            error);
    }

    [Fact]
    public async Task Administradora_NoDebePoderDesactivarSuPropiaCuenta()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var context =
                     new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            context.Set<Usuario>().Add(
                new Usuario
                {
                    Id = 1,
                    NombreCompleto = "Administradora",
                    NombreUsuario = "admin",
                    PasswordHash = "hash-prueba",
                    Rol = RolUsuario.Administradora,
                    Activo = true
                });

            await context.SaveChangesAsync();
        }

        var factory =
            new TestDbContextFactory(options);

        var repository =
            new UsuarioRepository(factory);

        var sesion =
            new SesionUsuario();

        sesion.IniciarSesion(
            new UsuarioSesion
            {
                IdUsuario = 1,
                NombreCompleto = "Administradora",
                NombreUsuario = "admin",
                Rol = RolUsuario.Administradora
            });

        var useCase =
            new CambiarEstadoUsuarioUseCase(
                repository,
                sesion);

        var error =
            await useCase.EjecutarAsync(
                usuarioId: 1,
                activar: false);

        Assert.Equal(
            "No puede desactivar su propia cuenta.",
            error);
    }

    [Fact]
    public async Task Administradora_NoDebePoderCambiarSuPropioRol()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite(connection)
                .Options;

        await using (var context =
                     new BodegaLuchitoDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();

            context.Set<Usuario>().Add(
                new Usuario
                {
                    Id = 1,
                    NombreCompleto = "Administradora",
                    NombreUsuario = "admin",
                    PasswordHash = "hash-prueba",
                    Rol = RolUsuario.Administradora,
                    Activo = true
                });

            await context.SaveChangesAsync();
        }

        var factory =
            new TestDbContextFactory(options);

        var repository =
            new UsuarioRepository(factory);

        var sesion =
            new SesionUsuario();

        sesion.IniciarSesion(
            new UsuarioSesion
            {
                IdUsuario = 1,
                NombreCompleto = "Administradora",
                NombreUsuario = "admin",
                Rol = RolUsuario.Administradora
            });

        var useCase =
            new ModificarUsuarioUseCase(
                repository,
                sesion);

        var error =
            await useCase.EjecutarAsync(
                new ModificarUsuarioRequest
                {
                    Id = 1,
                    NombreCompleto = "Administradora",
                    NombreUsuario = "admin",
                    Rol = RolUsuario.Vendedora
                });

        Assert.Equal(
            "No puede modificar su propio rol.",
            error);
    }
}
