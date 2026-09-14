using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using Xunit;

namespace BodegaLuchito.Tests.Application.Productos;

public class RegistrarProductoUseCaseTests
{
    private class FakeProductoRepository : IProductoRepository
    {
        public List<Producto> ProductosGuardados { get; } = new();

        public Task AgregarAsync(Producto producto, CancellationToken ct = default)
        {
            ProductosGuardados.Add(producto);
            return Task.CompletedTask;
        }

        public Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default)
        {
            return Task.FromResult(ProductosGuardados.Any(p => p.CodigoBarras == codigoBarras));
        }

        public Task<IEnumerable<Producto>> ObtenerActivosAsync(CancellationToken ct = default)
        {
            return Task.FromResult(ProductosGuardados.Where(p => p.Activo));
        }
    }

    [Fact]
    public async Task EjecutarAsync_ProductoInventariableSinCodigo_RegistraCorrectamente()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galletas", "Snacks", "   ", 1.50m, UnidadVenta.Unidad, true, 10, 5);

        await useCase.EjecutarAsync(request);

        var guardado = repository.ProductosGuardados.Single();
        Assert.Null(guardado.CodigoBarras);
    }

    [Fact]
    public async Task EjecutarAsync_ProductoVendidoPorPeso_RegistraCorrectamente()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Tomate", "Verduras", null, 4.50m, UnidadVenta.Peso, true, 15.5m, 2.0m);

        await useCase.EjecutarAsync(request);

        var guardado = repository.ProductosGuardados.Single();
        Assert.Equal(UnidadVenta.Peso, guardado.UnidadVenta);
        Assert.Equal(15.5m, guardado.StockActual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task EjecutarAsync_NombreVacio_LanzaArgumentException(string? nombre)
    {
        var useCase = new RegistrarProductoUseCase(new FakeProductoRepository());
        var request = new RegistrarProductoRequest(nombre!, "Cat", null, 1m, UnidadVenta.Unidad, false, 0, 0);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("nombre", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_CategoriaVacia_LanzaArgumentException()
    {
        var useCase = new RegistrarProductoUseCase(new FakeProductoRepository());
        var request = new RegistrarProductoRequest("Pan", " ", null, 1m, UnidadVenta.Unidad, false, 0, 0);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("categoría", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_StockActualNegativo_LanzaArgumentException()
    {
        var useCase = new RegistrarProductoUseCase(new FakeProductoRepository());
        var request = new RegistrarProductoRequest("Pan", "Cat", null, 1m, UnidadVenta.Unidad, true, -1, 5);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("actual no puede ser negativo", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_StockMinimoNegativo_LanzaArgumentException()
    {
        var useCase = new RegistrarProductoUseCase(new FakeProductoRepository());
        var request = new RegistrarProductoRequest("Pan", "Cat", null, 1m, UnidadVenta.Unidad, true, 10, -1);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("mínimo no puede ser negativo", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_CodigoBarrasDuplicado_LanzaInvalidOperationException()
    {
        var repository = new FakeProductoRepository();
        await repository.AgregarAsync(new Producto { CodigoBarras = "123" });
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Pan", "Cat", "123", 1m, UnidadVenta.Unidad, false, 0, 0);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("ya se encuentra registrado", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_SinControlInventario_AsignaStocksCero()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Pan", "Cat", null, 1m, UnidadVenta.Unidad, false, 10, 5);

        await useCase.EjecutarAsync(request);

        var guardado = repository.ProductosGuardados.Single();
        Assert.Equal(0m, guardado.StockActual);
        Assert.Equal(0m, guardado.StockMinimo);
    }
}
