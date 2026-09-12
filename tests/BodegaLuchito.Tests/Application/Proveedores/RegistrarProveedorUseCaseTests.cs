using BodegaLuchito.Application.Proveedores.DTOs;
using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Application.Proveedores.UseCases;
using BodegaLuchito.Domain.Proveedores.Entities;
using Xunit;

namespace BodegaLuchito.Tests.Application.Proveedores;

public class RegistrarProveedorUseCaseTests
{
    // 1. Creamos nuestro propio "Fake" (Simulador) del repositorio usando solo C#
    private class FakeProveedorRepository : IProveedorRepository
    {
        public List<Proveedor> BaseDeDatosSimulada { get; } = new();

        public Task RegistrarAsync(Proveedor proveedor)
        {
            BaseDeDatosSimulada.Add(proveedor);
            return Task.CompletedTask;
        }

        public Task<bool> ExisteRucAsync(string ruc)
        {
            return Task.FromResult(BaseDeDatosSimulada.Any(p => p.Ruc == ruc));
        }

        public Task<List<Proveedor>> ListarActivosAsync()
        {
            return Task.FromResult(BaseDeDatosSimulada.Where(p => p.Activo).ToList());
        }
    }

    // 2. Variables para nuestras pruebas
    private readonly FakeProveedorRepository _fakeRepository;
    private readonly RegistrarProveedorUseCase _useCase;

    public RegistrarProveedorUseCaseTests()
    {
        _fakeRepository = new FakeProveedorRepository();
        _useCase = new RegistrarProveedorUseCase(_fakeRepository);
    }

    [Fact]
    public async Task Registrar_ConDatosValidos_RetornaProveedorActivo()
    {
        // Arrange
        var request = new RegistrarProveedorRequest { Nombre = "Distribuidora XYZ", Ruc = "10456789123" };

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Distribuidora XYZ", result.Nombre);
        Assert.True(result.Activo);
        // Comprobamos que físicamente se agregó a la lista de nuestro Fake
        Assert.Single(_fakeRepository.BaseDeDatosSimulada);
    }

    [Fact]
    public async Task Registrar_ConNombreVacio_LanzaExcepcion()
    {
        // Arrange
        var request = new RegistrarProveedorRequest { Nombre = "" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request));
        Assert.Contains("requerido", ex.Message);
    }

    [Fact]
    public async Task Registrar_ConRucFormatoIncorrecto_LanzaExcepcion()
    {
        // Arrange
        var request = new RegistrarProveedorRequest { Nombre = "Prov", Ruc = "123" }; // Solo 3 dígitos

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _useCase.ExecuteAsync(request));
        Assert.Contains("11 dígitos", ex.Message);
    }

    [Fact]
    public async Task Registrar_ConRucDuplicado_LanzaExcepcion()
    {
        // Arrange
        // Pre-cargamos un proveedor en nuestro Fake con el mismo RUC simulando la BD
        _fakeRepository.BaseDeDatosSimulada.Add(new Proveedor { Nombre = "Existente", Ruc = "10456789123" });

        var request = new RegistrarProveedorRequest { Nombre = "Prov", Ruc = "10456789123" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.ExecuteAsync(request));
        Assert.Contains("registrado", ex.Message);
    }
}
