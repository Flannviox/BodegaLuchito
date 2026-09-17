using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BodegaLuchito.Tests.Infraestructure.Persistence;

public sealed class ProveedorMigracionesTests
{
    private const string MigracionAnterior = "20260914052304_IntegrarProveedoresCaja";

    [Fact]
    public async Task Migrar_ProveedorConRuc_ConservaDatosYModeloSinCambiosPendientes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite(connection).Options;

        await using (var context = new BodegaLuchitoDbContext(options))
        {
            await context.GetService<IMigrator>().MigrateAsync(MigracionAnterior);
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO Proveedor (Nombre, Ruc, Telefono, Direccion, Activo, FechaCreacion)
                VALUES ('Proveedor anterior', '10456789123', '1234567890', 'Direccion anterior', 1, '2026-09-14 10:00:00');
                """);
            await context.Database.MigrateAsync();
            Assert.False(context.Database.HasPendingModelChanges());
            Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        }

        await using var verification = new BodegaLuchitoDbContext(options);
        var proveedor = await verification.Set<Proveedor>().SingleAsync();
        Assert.Equal("Proveedor anterior", proveedor.Nombre);
        Assert.Equal("10456789123", proveedor.Ruc);
        Assert.Equal("1234567890", proveedor.Telefono);
        Assert.Equal("Direccion anterior", proveedor.Direccion);
        Assert.True(proveedor.Activo);
        Assert.Null(proveedor.FechaActualizacion);

        await Assert.ThrowsAsync<SqliteException>(() => verification.Database.ExecuteSqlRawAsync("""
            INSERT INTO Proveedor (Nombre, Ruc, Activo, FechaCreacion)
            VALUES ('Proveedor sin RUC', NULL, 1, '2026-09-16 10:00:00');
            """));
    }

    [Fact]
    public async Task Migrar_ProveedorAnteriorSinRuc_NoInventaRucNiPierdeDatos()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite(connection).Options;

        await using var context = new BodegaLuchitoDbContext(options);
        await context.GetService<IMigrator>().MigrateAsync(MigracionAnterior);
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO Proveedor (Nombre, Ruc, Activo, FechaCreacion)
            VALUES ('Pendiente uno', NULL, 1, '2026-09-14 10:00:00'),
                   ('Pendiente dos', NULL, 1, '2026-09-14 10:00:00');
            """);

        await Assert.ThrowsAsync<SqliteException>(() => context.Database.MigrateAsync());

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM Proveedor WHERE Ruc IS NULL;";
        Assert.Equal(2L, (long)(await command.ExecuteScalarAsync())!);
        Assert.Contains(await context.Database.GetPendingMigrationsAsync(),
            migration => migration.EndsWith("ProveedorRucObligatorioTelefono10", StringComparison.Ordinal));

        // Al completar los RUC reales, la misma actualizacion puede reintentarse.
        await context.Database.ExecuteSqlRawAsync("""
            UPDATE Proveedor SET Ruc = '10456789123' WHERE Nombre = 'Pendiente uno';
            UPDATE Proveedor SET Ruc = '10456789124' WHERE Nombre = 'Pendiente dos';
            """);
        await context.Database.MigrateAsync();
        Assert.Equal(2, await context.Set<Proveedor>().CountAsync());
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }
}
