using BodegaLuchito.Application.Proveedores.DTOs;
using BodegaLuchito.Application.Proveedores.UseCases;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Proveedores.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BodegaLuchito.Tests.Application.Proveedores;

public class ProveedorUseCasesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<BodegaLuchitoDbContext> _options;
    private readonly FakeDbContextFactory _factory;
    private readonly ProveedorRepository _repository;

    // Casos de Uso
    private readonly RegistrarProveedorUseCase _registrarUseCase;
    private readonly EditarProveedorUseCase _editarUseCase;
    private readonly CambiarEstadoProveedorUseCase _cambiarEstadoUseCase;
    private readonly EliminarProveedorUseCase _eliminarUseCase;

    // Fake Factory para inyectar DbContext en memoria
    private class FakeDbContextFactory : IDbContextFactory<BodegaLuchitoDbContext>
    {
        private readonly DbContextOptions<BodegaLuchitoDbContext> _options;
        public FakeDbContextFactory(DbContextOptions<BodegaLuchitoDbContext> options) => _options = options;
        public BodegaLuchitoDbContext CreateDbContext() => new BodegaLuchitoDbContext(_options);
    }

    public ProveedorUseCasesTests()
    {
        // 1. Configuración de SQLite en Memoria
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
            .UseSqlite(_connection)
            .Options;

        // 2. Crear esquema temporal
        using (var context = new BodegaLuchitoDbContext(_options))
        {
            context.Database.Migrate();
        }

        // 3. Inicializar Repositorio y Casos de Uso
        _factory = new FakeDbContextFactory(_options);
        _repository = new ProveedorRepository(_factory);
        _registrarUseCase = new RegistrarProveedorUseCase(_repository);
        _editarUseCase = new EditarProveedorUseCase(_repository);
        _cambiarEstadoUseCase = new CambiarEstadoProveedorUseCase(_repository);
        _eliminarUseCase = new EliminarProveedorUseCase(_repository);
    }

    // =========================================================================
    // PRUEBAS DE REGISTRO (RegistrarProveedorUseCase)
    // =========================================================================

    [Fact]
    public async Task Registrar_DatosCompletos_GuardaCorrectamente()
    {
        var request = new RegistrarProveedorRequest
        {
            Nombre = "Proveedor SAC",
            Ruc = "10456789123",
            Telefono = "987654321",
            Direccion = "Av. Principal 123"
        };

        var result = await _registrarUseCase.ExecuteAsync(request);

        Assert.Equal("Proveedor SAC", result.Nombre);
        Assert.Equal("10456789123", result.Ruc);
        Assert.Equal("987654321", result.Telefono);
        Assert.True(result.Activo);
        Assert.Null(result.FechaActualizacion);
    }

    [Fact]
    public async Task Registrar_CamposOpcionalesVacios_NormalizaANull()
    {
        var request = new RegistrarProveedorRequest
        {
            Nombre = " Prov Espacios ",
            Ruc = " 10456789124 ",
            Telefono = "  ",
            Direccion = ""
        };

        var result = await _registrarUseCase.ExecuteAsync(request);

        Assert.Equal("Prov Espacios", result.Nombre);
        Assert.Equal("10456789124", result.Ruc);
        Assert.Null(result.Telefono);
        Assert.Null(result.Direccion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Registrar_NombreVacio_LanzaExcepcion(string? nombreInvalido)
    {
        var request = new RegistrarProveedorRequest { Nombre = nombreInvalido!, Ruc = "10456789123" };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _registrarUseCase.ExecuteAsync(request));
        Assert.Contains("requerido", ex.Message);
    }

    [Fact]
    public async Task Registrar_NombreExcede150Caracteres_LanzaExcepcion()
    {
        var request = new RegistrarProveedorRequest { Nombre = new string('X', 151), Ruc = "10456789123" };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _registrarUseCase.ExecuteAsync(request));
        Assert.Contains("150 caracteres", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Registrar_RucVacio_LanzaExcepcion(string? rucInvalido)
    {
        var request = new RegistrarProveedorRequest { Nombre = "Prov", Ruc = rucInvalido };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _registrarUseCase.ExecuteAsync(request));
        Assert.Contains("RUC es obligatorio", ex.Message);
    }

    [Theory]
    [InlineData("1234567890")] // 10 dígitos
    [InlineData("123456789012")] // 12 dígitos
    [InlineData("10456789ABC")] // Con letras
    [InlineData("1234567890١")] // Solo se aceptan digitos ASCII
    public async Task Registrar_RucFormatoInvalido_LanzaExcepcion(string rucInvalido)
    {
        var request = new RegistrarProveedorRequest { Nombre = "Prov", Ruc = rucInvalido };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _registrarUseCase.ExecuteAsync(request));
        Assert.Contains("11 dígitos", ex.Message);
    }

    [Fact]
    public async Task Registrar_RucDuplicado_LanzaExcepcion()
    {
        await _registrarUseCase.ExecuteAsync(new RegistrarProveedorRequest { Nombre = "P1", Ruc = "10000000001" });
        var request = new RegistrarProveedorRequest { Nombre = "P2", Ruc = "10000000001" };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _registrarUseCase.ExecuteAsync(request));
        Assert.Contains("registrado", ex.Message);
    }

    [Theory]
    [InlineData("12345678901")] // 11 dígitos (falla porque el límite es 10)
    [InlineData("987654ABC")] // Con letras
    public async Task Registrar_TelefonoInvalido_LanzaExcepcion(string? telefonoInvalido)
    {
        var request = new RegistrarProveedorRequest { Nombre = "P1", Ruc = "10000000001", Telefono = telefonoInvalido };
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _registrarUseCase.ExecuteAsync(request));
        Assert.Contains("10 dígitos", ex.Message);
    }

    // =========================================================================
    // PRUEBAS DE EDICIÓN (EditarProveedorUseCase)
    // =========================================================================

    [Fact]
    public async Task Editar_DatosValidos_ActualizaCorrectamente()
    {
        var prov = await _registrarUseCase.ExecuteAsync(new RegistrarProveedorRequest { Nombre = "P1", Ruc = "10000000001" });

        await _editarUseCase.ExecuteAsync(prov.Id, "999888777", "Nueva Direccion");
        var editado = await _repository.ObtenerPorIdAsync(prov.Id);

        Assert.NotNull(editado);
        Assert.Equal("999888777", editado.Telefono);
        Assert.Equal("Nueva Direccion", editado.Direccion);
        Assert.NotNull(editado.FechaActualizacion);
    }

    [Fact]
    public async Task Editar_ProveedorInexistente_LanzaExcepcion()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _editarUseCase.ExecuteAsync(999, "987", "Dir"));
        Assert.Contains("no encontrado", ex.Message);
    }

    [Fact]
    public async Task Editar_TelefonoInvalido_LanzaExcepcion()
    {
        var prov = await _registrarUseCase.ExecuteAsync(new RegistrarProveedorRequest { Nombre = "P1", Ruc = "10000000001" });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _editarUseCase.ExecuteAsync(prov.Id, "12345678901", "Dir"));
        Assert.Contains("10 dígitos", ex.Message);
    }

    // =========================================================================
    // PRUEBAS DE CAMBIO DE ESTADO (CambiarEstadoProveedorUseCase)
    // =========================================================================

    [Fact]
    public async Task CambiarEstado_DesactivarYActivar_ActualizaCorrectamente()
    {
        var prov = await _registrarUseCase.ExecuteAsync(new RegistrarProveedorRequest { Nombre = "P1", Ruc = "10000000001" });

        await _cambiarEstadoUseCase.ExecuteAsync(prov.Id, false);
        var inhabilitado = await _repository.ObtenerPorIdAsync(prov.Id);
        Assert.False(inhabilitado!.Activo);
        Assert.NotNull(inhabilitado.FechaActualizacion);

        await _cambiarEstadoUseCase.ExecuteAsync(prov.Id, true);
        var activado = await _repository.ObtenerPorIdAsync(prov.Id);
        Assert.True(activado!.Activo);
    }

    [Fact]
    public async Task CambiarEstado_ProveedorInexistente_LanzaExcepcion()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _cambiarEstadoUseCase.ExecuteAsync(999, false));
        Assert.Contains("no encontrado", ex.Message);
    }

    // =========================================================================
    // PRUEBAS DE ELIMINACIÓN (EliminarProveedorUseCase)
    // =========================================================================

    [Fact]
    public async Task Eliminar_ProveedorExistente_EliminaDeBD()
    {
        var prov = await _registrarUseCase.ExecuteAsync(new RegistrarProveedorRequest { Nombre = "P1", Ruc = "10000000001" });

        await _eliminarUseCase.ExecuteAsync(prov.Id);
        var eliminado = await _repository.ObtenerPorIdAsync(prov.Id);

        Assert.Null(eliminado);
    }

    [Fact]
    public async Task Eliminar_ProveedorInexistente_LanzaExcepcion()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _eliminarUseCase.ExecuteAsync(999));
        Assert.Contains("no encontrado", ex.Message);
    }

    // =========================================================================
    // LIMPIEZA DE CONEXIÓN
    // =========================================================================
    [Theory]
    [InlineData("1")]
    [InlineData("987654321")]
    [InlineData("1234567890")]
    public async Task RegistrarYEditar_TelefonoHasta10Digitos_Persiste(string telefono)
    {
        var proveedor = await _registrarUseCase.ExecuteAsync(new RegistrarProveedorRequest
        {
            Nombre = "Proveedor limite",
            Ruc = "10456789123",
            Telefono = $" {telefono} ",
            Direccion = new string('D', 200)
        });

        var guardado = await _repository.ObtenerPorIdAsync(proveedor.Id);
        Assert.Equal(telefono, guardado!.Telefono);
        Assert.Equal(200, guardado.Direccion!.Length);

        await _editarUseCase.ExecuteAsync(proveedor.Id, null, null);
        await _editarUseCase.ExecuteAsync(proveedor.Id, $" {telefono} ", new string('E', 200));
        var editado = await _repository.ObtenerPorIdAsync(proveedor.Id);
        Assert.Equal(telefono, editado!.Telefono);
        Assert.Equal(200, editado.Direccion!.Length);
    }

    [Fact]
    public async Task Registrar_DireccionMayorA200_NoGuardaProveedor()
    {
        var request = new RegistrarProveedorRequest
        {
            Nombre = "Proveedor",
            Ruc = "10456789123",
            Direccion = new string('D', 201)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _registrarUseCase.ExecuteAsync(request));
        Assert.Empty(await _repository.ObtenerTodosAsync());
    }

    [Fact]
    public async Task Editar_DireccionMayorA200_ConservaDatosAnteriores()
    {
        var proveedor = await _registrarUseCase.ExecuteAsync(new RegistrarProveedorRequest
        {
            Nombre = "Proveedor",
            Ruc = "10456789123",
            Telefono = "987654321",
            Direccion = "Direccion original"
        });

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _editarUseCase.ExecuteAsync(proveedor.Id, "1234567890", new string('D', 201)));

        var guardado = await _repository.ObtenerPorIdAsync(proveedor.Id);
        Assert.Equal("Direccion original", guardado!.Direccion);
        Assert.Equal("987654321", guardado.Telefono);
        Assert.Null(guardado.FechaActualizacion);
    }

    // =========================================================================
    // LIMPIEZA DE CONEXIÓN
    // =========================================================================

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
