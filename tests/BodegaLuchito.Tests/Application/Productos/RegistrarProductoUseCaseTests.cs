using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using Xunit;

namespace BodegaLuchito.Tests.Application.Productos;

public class RegistrarProductoUseCaseTests
{
    // Fake Repository para simular la base de datos en memoria sin librerías externas
    private class FakeProductoRepository : IProductoRepository
    {
        public List<Producto> ProductosGuardados { get; } = new();

        public Task AgregarAsync(Producto producto)
        {
            ProductosGuardados.Add(producto);
            return Task.CompletedTask;
        }

        public Task<bool> ExisteCodigoBarrasAsync(string codigoBarras)
        {
            return Task.FromResult(ProductosGuardados.Any(p => p.CodigoBarras == codigoBarras));
        }

        public Task<IEnumerable<Producto>> ObtenerActivosAsync()
        {
            return Task.FromResult(ProductosGuardados.Where(p => p.Activo));
        }
    }

    [Fact]
    public async Task EjecutarAsync_ProductoValido_RegistraCorrectamente()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest(
            "Arroz Extra 1kg", "Abarrotes", "77512345", 3.80m, UnidadVenta.Unidad, true, 50, 10);

        await useCase.EjecutarAsync(request);

        Assert.Single(repository.ProductosGuardados);
        var guardado = repository.ProductosGuardados.First();
        Assert.Equal("Arroz Extra 1kg", guardado.Nombre);
        Assert.Equal("77512345", guardado.CodigoBarras);
        Assert.True(guardado.Activo);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EjecutarAsync_NombreVacio_LanzaArgumentException(string? nombreInvalido)
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest(
            nombreInvalido!, "Abarrotes", "123", 1m, UnidadVenta.Unidad, false, 0, 0);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("El nombre del producto es obligatorio", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5.5)]
    public async Task EjecutarAsync_PrecioVentaInvalido_LanzaArgumentException(decimal precioInvalido)
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest(
            "Fideos", "Abarrotes", "123", precioInvalido, UnidadVenta.Unidad, false, 0, 0);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("El precio de venta debe ser mayor a cero", exception.Message);
    }

    [Fact]
    public async Task EjecutarAsync_CodigoBarrasDuplicado_LanzaInvalidOperationException()
    {
        var repository = new FakeProductoRepository();
        // Pre-cargamos un producto con el código "775000"
        await repository.AgregarAsync(new Producto { CodigoBarras = "775000" });

        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest(
            "Galletas", "Snacks", "775000", 1.50m, UnidadVenta.Unidad, false, 0, 0);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("ya se encuentra registrado", exception.Message);
    }

    [Fact]
    public async Task EjecutarAsync_SinControlInventario_AsignaStocksCero()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);

        // Enviamos stocks aleatorios pero indicamos que NO controla inventario
        var request = new RegistrarProductoRequest(
            "Plátano", "Frutas", null, 4.50m, UnidadVenta.Peso, false, 100, 50);

        await useCase.EjecutarAsync(request);

        var guardado = repository.ProductosGuardados.First();
        Assert.False(guardado.ControlaInventario);
        Assert.Equal(0m, guardado.StockActual);
        Assert.Equal(0m, guardado.StockMinimo);
        Assert.Null(guardado.CodigoBarras);
    }

    [Fact]
    public async Task EjecutarAsync_ControlaInventarioSinCodigo_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);

        // ControlaInventario = true, pero CodigoBarras = null
        var request = new RegistrarProductoRequest(
            "Galletas", "Snacks", null, 1.50m, UnidadVenta.Unidad, true, 10, 5);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("código de barras es obligatorio", exception.Message);
    }
}
