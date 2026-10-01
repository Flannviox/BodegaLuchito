using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Application.Productos.UseCases;

public sealed class ConsultarHistorialPreciosProductoUseCase
{
    private readonly IProductoRepository _productoRepository;

    public ConsultarHistorialPreciosProductoUseCase(
        IProductoRepository productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public Task<IReadOnlyList<HistorialPrecioProducto>> EjecutarAsync(
        int productoId,
        CancellationToken cancellationToken = default)
    {
        if (productoId <= 0)
            throw new ArgumentException("El producto es obligatorio.", nameof(productoId));

        return _productoRepository.ObtenerHistorialPreciosAsync(
            productoId,
            cancellationToken);
    }
}
