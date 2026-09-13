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
    private readonly BodegaLuchitoDbContext _context;
    private readonly ProveedorRepository _repository;
    private readonly RegistrarProveedorUseCase _useCase;

    public RegistrarProveedorUseCaseTests()
    {
        // 1. Crear base de datos provisional en memoria con SQLite
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open(); // Se debe mantener abierta durante la prueba

        var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new BodegaLuchitoDbContext(options);

        // 2. Generar el esquema provisional (tablas) sin usar migraciones
        _context.Database.EnsureCreated();

        // 3. Inicializar repositorio real y caso de uso
        _repository = new ProveedorRepository(_context);
        _useCase = new RegistrarProveedorUseCase(_repository);
    }

    [Fact]
    public async Task Registrar_ConDatosValidos_GuardaEnBDProvisional()
    {
        // Arrange
        var request = new RegistrarProveedorRequest
        {
            Nombre = "Distribuidora Luchito",
            Ruc = "10456789123",
            Telefono = "987654321",
            Direccion = "Av. Las Flores 123"
        };

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0); // Confirma que la BD provisional generó un ID

        // Verificar directamente en la BD provisional
        var proveedorEnBd = await _context.Set<Proveedor>().FindAsync(result.Id);
        Assert.NotNull(proveedorEnBd);
        Assert.Equal("Distribuidora Luchito", proveedorEnBd.Nombre);
    }

    [Fact]
    public async Task Registrar_ConNumerosEnNombre_LanzaExcepcion()
    {
        // Arrange
        var request = new RegistrarProveedorRequest { Nombre = "Distribuidora 123" }; // Tiene números

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request));
        Assert.Contains("letras y espacios", ex.Message);
    }

    [Fact]
    public async Task Registrar_ConLetrasEnRuc_LanzaExcepcion()
    {
        // Arrange
        var request = new RegistrarProveedorRequest { Nombre = "Proveedor", Ruc = "104567ABCDE" }; // Contiene letras

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request));
        Assert.Contains("11 dígitos numéricos", ex.Message);
    }

    [Fact]
    public async Task Registrar_ConTelefonoIncorrecto_LanzaExcepcion()
    {
        // Arrange
        var request = new RegistrarProveedorRequest { Nombre = "Proveedor", Telefono = "98765432" }; // Solo 8 dígitos

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request));
        Assert.Contains("9 dígitos numéricos", ex.Message);
    }

    [Fact]
    public async Task Registrar_ConRucDuplicado_LanzaExcepcion()
    {
        // Arrange
        // Guardamos un proveedor inicial en la BD provisional
        _context.Set<Proveedor>().Add(new Proveedor { Nombre = "Existente", Ruc = "10456789123" });
        await _context.SaveChangesAsync();

        var request = new RegistrarProveedorRequest { Nombre = "Nuevo Prov", Ruc = "10456789123" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
        Assert.Contains("registrado", ex.Message);
    }

    // Se ejecuta al terminar cada test para limpiar la conexión
    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
        _connection.Close();
    }
}
