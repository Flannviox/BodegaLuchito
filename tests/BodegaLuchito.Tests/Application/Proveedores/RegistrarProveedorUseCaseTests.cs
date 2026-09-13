using BodegaLuchito.Application.Proveedores.DTOs;
using BodegaLuchito.Application.Proveedores.UseCases;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Proveedores.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BodegaLuchito.Tests.Application.Proveedores;

public class RegistrarProveedorUseCaseTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<BodegaLuchitoDbContext> _options;
    private readonly FakeDbContextFactory _factory;
    private readonly ProveedorRepository _repository;
    private readonly RegistrarProveedorUseCase _useCase;

    // Fake para reemplazar IDbContextFactory
    private class FakeDbContextFactory : IDbContextFactory<BodegaLuchitoDbContext>
    {
        private readonly DbContextOptions<BodegaLuchitoDbContext> _options;
        public FakeDbContextFactory(DbContextOptions<BodegaLuchitoDbContext> options) => _options = options;
        public BodegaLuchitoDbContext CreateDbContext() => new BodegaLuchitoDbContext(_options);
    }

    public RegistrarProveedorUseCaseTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite(_connection)
            .Options;

        using (var context = new BodegaLuchitoDbContext(_options))
        {
            context.Database.EnsureCreated();
        }

        _factory = new FakeDbContextFactory(_options);
        _repository = new ProveedorRepository(_factory);
        _useCase = new RegistrarProveedorUseCase(_repository);
    }

    [Fact]
    public async Task Registrar_ConRazonSocialRealYActivo_GuardaCorrectamente()
    {
        var request = new RegistrarProveedorRequest { Nombre = "Distribuidora 3 Hermanos S.A.C." };
        var result = await _useCase.ExecuteAsync(request);

        Assert.Equal("Distribuidora 3 Hermanos S.A.C.", result.Nombre);
        Assert.True(result.Activo);
        Assert.Null(result.FechaActualizacion);
    }

    [Fact]
    public async Task Registrar_NombreVacioONull_LanzaExcepcion()
    {
        var request1 = new RegistrarProveedorRequest { Nombre = "" };
        var request2 = new RegistrarProveedorRequest { Nombre = "   " };

        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request1));
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request2));
    }

    [Fact]
    public async Task Registrar_NombreLargo_LanzaExcepcion()
    {
        var request = new RegistrarProveedorRequest { Nombre = new string('A', 151) };
        await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request));
    }

    [Fact]
    public async Task Registrar_NormalizaCampos_GuardandoVaciosComoNull()
    {
        var request = new RegistrarProveedorRequest
        {
            Nombre = " Proveedor Normalizado ",
            Ruc = "  ",
            Telefono = "",
            Direccion = " "
        };

        var result = await _useCase.ExecuteAsync(request);

        Assert.Equal("Proveedor Normalizado", result.Nombre);
        Assert.Null(result.Ruc);
        Assert.Null(result.Telefono);
        Assert.Null(result.Direccion);
    }

    [Fact]
    public async Task Registrar_VariosProveedoresSinRuc_SeGuardanCorrectamente()
    {
        await _useCase.ExecuteAsync(new RegistrarProveedorRequest { Nombre = "Prov 1", Ruc = null });
        var result2 = await _useCase.ExecuteAsync(new RegistrarProveedorRequest { Nombre = "Prov 2", Ruc = null });

        using var context = _factory.CreateDbContext();
        var count = await context.Set<Proveedor>().CountAsync();

        Assert.Equal(2, count);
        Assert.Null(result2.Ruc);
    }

    [Fact]
    public async Task Registrar_RucDuplicado_LanzaExcepcion()
    {
        await _useCase.ExecuteAsync(new RegistrarProveedorRequest { Nombre = "Prov 1", Ruc = "12345678901" });
        var request = new RegistrarProveedorRequest { Nombre = "Prov 2", Ruc = "12345678901" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
        Assert.Contains("registrado", ex.Message);
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
