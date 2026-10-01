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

        public Task<IEnumerable<Producto>> ObtenerInactivosAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Enumerable.Empty<Producto>());
        }

        public Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Producto?>(null);
        }

        public Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ActualizarConHistorialPrecioAsync(
            Producto producto,
            HistorialPrecioProducto? historialPrecio,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistorialPrecioProducto>> ObtenerHistorialPreciosAsync(
            int productoId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<HistorialPrecioProducto>>(
                Array.Empty<HistorialPrecioProducto>());
        }

        public Task<IReadOnlyList<HistorialPrecioProducto>> ObtenerHistorialPreciosGlobalAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<HistorialPrecioProducto>>(
                Array.Empty<HistorialPrecioProducto>());
        }

        public Task ActualizarEstadoConHistorialAsync(
            Producto producto,
            HistorialActividadProducto historialActividad,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<HistorialActividadProducto>> ObtenerHistorialActividadAsync(
            int productoId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<HistorialActividadProducto>>(
                Array.Empty<HistorialActividadProducto>());
        }

        public Task<IReadOnlyList<HistorialActividadProducto>> ObtenerHistorialActividadGlobalAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<HistorialActividadProducto>>(
                Array.Empty<HistorialActividadProducto>());
        }
    }

    [Fact]
    public async Task EjecutarAsync_DatosValidos_AgregaProducto()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", 1, "123", 1.5m, UnidadVenta.Unidad, true, 10, 5); // 1 = CategoriaId

        await useCase.EjecutarAsync(request);

        Assert.NotNull(repository.ProductoAgregado);
        Assert.Equal("Galleta", repository.ProductoAgregado.Nombre);
        Assert.Equal(0m, repository.ProductoAgregado.StockActual);
    }

    [Fact]
    public async Task EjecutarAsync_NombreVacio_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("", 1, "123", 1.5m, UnidadVenta.Unidad, true, 10, 5);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task EjecutarAsync_CategoriaVacia_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", 0, "123", 1.5m, UnidadVenta.Unidad, true, 10, 5); // 0 = Id Inválido

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task EjecutarAsync_PrecioInvalido_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", 1, "123", 0m, UnidadVenta.Unidad, true, 10, 5);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task EjecutarAsync_StockInicialIngresado_SeRegistraEnCero()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", 1, "123", 1.5m, UnidadVenta.Unidad, true, 20, 5);

        await useCase.EjecutarAsync(request);

        Assert.NotNull(repository.ProductoAgregado);
        Assert.Equal(0m, repository.ProductoAgregado.StockActual);
    }

    [Fact]
    public async Task EjecutarAsync_StockMinimoNegativo_LanzaArgumentException()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", 1, "123", 1.5m, UnidadVenta.Unidad, true, 10, -5);

        await Assert.ThrowsAsync<ArgumentException>(() => useCase.EjecutarAsync(request));
    }

    [Fact]
    public async Task EjecutarAsync_CodigoBarrasDuplicado_LanzaInvalidOperationException()
    {
        var repository = new FakeProductoRepository { ExisteCodigo = true };
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Galleta", 1, "123", 1.5m, UnidadVenta.Unidad, true, 10, 5);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => useCase.EjecutarAsync(request));
        Assert.Contains("ya se encuentra", ex.Message);
    }

    [Fact]
    public async Task EjecutarAsync_AunSiLaSolicitudNoLoIndica_ControlaInventario()
    {
        var repository = new FakeProductoRepository();
        var useCase = new RegistrarProductoUseCase(repository);
        var request = new RegistrarProductoRequest("Servicio Delivery", 1, null, 10m, UnidadVenta.Unidad, false, 50, 50);

        await useCase.EjecutarAsync(request);

        Assert.NotNull(repository.ProductoAgregado);
        Assert.True(repository.ProductoAgregado.ControlaInventario);
        Assert.Equal(0m, repository.ProductoAgregado.StockActual);
        Assert.Equal(50m, repository.ProductoAgregado.StockMinimo);
    }
}
