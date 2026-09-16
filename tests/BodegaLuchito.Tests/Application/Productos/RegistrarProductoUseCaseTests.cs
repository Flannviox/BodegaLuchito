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

public sealed class RegistrarProductoUseCaseTests
{
    // =========================================================================
    // 1. REPOSITORIO FALSO ACTUALIZADO
    // =========================================================================
    private class FakeProductoRepository : IProductoRepository
    {
        public bool ExisteCodigo = false;
        public Producto? ProductoAgregado;

        public Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            ProductoAgregado = producto;
            return Task.CompletedTask;
        }

        public Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ExisteCodigo);
        }

        public Task<IEnumerable<Producto>> ObtenerActivosAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<Producto>());
        }

        // --- NUEVOS MÉTODOS REQUERIDOS POR LA INTERFAZ ---
        public Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Producto?>(null);
        }

        public Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    // =========================================================================
    // 2. PRUEBAS UNITARIAS DEL SPRINT 01
    // =========================================================================

    [Fact]
    public async Task EjecutarAsync_DatosValidos_AgregaProducto()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", "Abarrotes", "123", 1.5m, UnidadVenta.Unidad, true, 10, 5);

        await useCase.EjecutarAsync(request);

        Assert.NotNull(repository.ProductoAgregado);
        Assert.Equal("Galleta", repository.ProductoAgregado.Nombre);
    }

    [Fact]
    public async Task EjecutarAsync_NombreVacio_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("", "Abarrotes", "123", 1.5m, UnidadVenta.Unidad, true, 10, 5);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task EjecutarAsync_CategoriaVacia_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", "", "123", 1.5m, UnidadVenta.Unidad, true, 10, 5);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task EjecutarAsync_PrecioInvalido_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", "Abarrotes", "123", 0m, UnidadVenta.Unidad, true, 10, 5);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task EjecutarAsync_StockActualNegativo_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", "Abarrotes", "123", 1.5m, UnidadVenta.Unidad, true, -1, 5);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task EjecutarAsync_StockMinimoNegativo_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", "Abarrotes", "123", 1.5m, UnidadVenta.Unidad, true, 10, -5);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task EjecutarAsync_CodigoBarrasDuplicado_LanzaInvalidOperationException()
    {
        var repository = new FakeProductoRepository { ExisteCodigo = true };
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", "Abarrotes", "123", 1.5m, UnidadVenta.Unidad, true, 10, 5);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("ya se encuentra", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_SinControlInventario_AsignaStocksCero()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Servicio Delivery", "Servicios", null, 10m, UnidadVenta.Unidad, false, 50, 50);

        await useCase.EjecutarAsync(request);

        Assert.NotNull(repository.ProductoAgregado);
        Assert.False(repository.ProductoAgregado.ControlaInventario);
        Assert.Equal(0m, repository.ProductoAgregado.StockActual);
        Assert.Equal(0m, repository.ProductoAgregado.StockMinimo);
    }
}
