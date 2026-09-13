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

public sealed class IniciarSesionUseCaseTests
{
    [Fact]
    public async Task CredencialesCorrectas_DebeIniciarSesion()
    {
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

        var hasher =
            new Pbkdf2PasswordHasher();

        var usuario = new Usuario
        {
            NombreCompleto = "Administradora Prueba",
            NombreUsuario = "admin",
            PasswordHash = hasher.GenerarHash("Admin123"),
            Rol = RolUsuario.Administradora,
            Activo = true
        };

        context.Set<Usuario>().Add(usuario);

        await context.SaveChangesAsync();

        var contextFactory =
    new TestDbContextFactory(options);

        var repository =
            new UsuarioRepository(contextFactory);

        var sesion =
            new SesionUsuario();

        var useCase =
            new IniciarSesionUseCase(
                repository,
                hasher,
                sesion);

        var request =
            new IniciarSesionRequest
            {
                NombreUsuario = "admin",
                Password = "Admin123"
            };

        var resultado =
            await useCase.EjecutarAsync(request);

        Assert.True(resultado.Exitoso);

        Assert.True(sesion.EstaAutenticado);

        Assert.NotNull(sesion.UsuarioActual);

        Assert.Equal(
            "admin",
            sesion.UsuarioActual!.NombreUsuario);

        Assert.Equal(
            "Administradora",
            sesion.UsuarioActual.NombreRol);
    }

    [Fact]
    public async Task PasswordIncorrecto_NoDebeIniciarSesion()
    {
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

        var hasher =
            new Pbkdf2PasswordHasher();

        var usuario = new Usuario
        {
            NombreCompleto = "Administradora Prueba",
            NombreUsuario = "admin",
            PasswordHash = hasher.GenerarHash("Admin123"),
            Rol = RolUsuario.Administradora,
            Activo = true
        };

        context.Set<Usuario>().Add(usuario);

        await context.SaveChangesAsync();

        var contextFactory =
            new TestDbContextFactory(options);

        var repository =
            new UsuarioRepository(contextFactory);

        var sesion =
            new SesionUsuario();

        var useCase =
            new IniciarSesionUseCase(
                repository,
                hasher,
                sesion);

        var request =
            new IniciarSesionRequest
            {
                NombreUsuario = "admin",
                Password = "PasswordIncorrecto"
            };

        var resultado =
            await useCase.EjecutarAsync(request);

        Assert.False(resultado.Exitoso);

        Assert.False(sesion.EstaAutenticado);

        Assert.Null(sesion.UsuarioActual);
    }
    [Fact]
    public async Task UsuarioDesactivado_NoDebeIniciarSesion()
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

            var hasher =
                new Pbkdf2PasswordHasher();

            context.Set<Usuario>().Add(
                new Usuario
                {
                    NombreCompleto = "Vendedora",
                    NombreUsuario = "vendedora",
                    PasswordHash =
                        hasher.GenerarHash("ClaveSegura123"),
                    Rol = RolUsuario.Vendedora,
                    Activo = false
                });

            await context.SaveChangesAsync();
        }

        var factory =
            new TestDbContextFactory(options);

        var repository =
            new UsuarioRepository(factory);

        var passwordHasher =
            new Pbkdf2PasswordHasher();

        var sesion =
            new SesionUsuario();

        var useCase =
            new IniciarSesionUseCase(
                repository,
                passwordHasher,
                sesion);

        var resultado =
            await useCase.EjecutarAsync(
                new IniciarSesionRequest
                {
                    NombreUsuario = "vendedora",
                    Password = "ClaveSegura123"
                });

        Assert.False(resultado.Exitoso);
        Assert.False(sesion.EstaAutenticado);
        Assert.Null(sesion.UsuarioActual);
    }
}
