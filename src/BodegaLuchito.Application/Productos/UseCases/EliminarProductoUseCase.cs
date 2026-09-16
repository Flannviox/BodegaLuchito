using System;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.Interfaces;

namespace BodegaLuchito.Application.Productos.UseCases;

public sealed class EliminarProductoUseCase
{
    private readonly IProductoRepository _productoRepository;

    public EliminarProductoUseCase(IProductoRepository productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task EjecutarAsync(int id, CancellationToken cancellationToken = default)
    {
        var producto = await _productoRepository.ObtenerPorIdAsync(id, cancellationToken);

        if (producto == null)
            throw new InvalidOperationException("El producto que intenta eliminar no existe en el sistema.");

        // cambiamos estado a inactivo en lugar de eliminarlo físicamente
        producto.Activo = false;

        await _productoRepository.ActualizarAsync(producto, cancellationToken);
    }
}
