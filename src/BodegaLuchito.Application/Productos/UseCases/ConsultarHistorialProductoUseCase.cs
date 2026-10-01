using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Application.Productos.UseCases;

public sealed class ConsultarHistorialProductoUseCase
{
    private readonly IProductoRepository _productoRepository;

    public ConsultarHistorialProductoUseCase(IProductoRepository productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task<IReadOnlyList<HistorialProductoItem>> EjecutarAsync(
        int productoId,
        CancellationToken cancellationToken = default)
    {
        if (productoId <= 0)
            throw new ArgumentException("El producto es obligatorio.", nameof(productoId));

        var historialPrecios = await _productoRepository.ObtenerHistorialPreciosAsync(
            productoId,
            cancellationToken);
        var historialActividad = await _productoRepository.ObtenerHistorialActividadAsync(
            productoId,
            cancellationToken);

        return historialPrecios
            .Select(x => new HistorialProductoItem(
                x.FechaHora,
                "Precio actualizado",
                $"Precio de venta: S/ {x.PrecioAnterior:F2} → S/ {x.PrecioNuevo:F2}",
                x.UsuarioNombre))
            .Concat(historialActividad.Select(x => new HistorialProductoItem(
                x.FechaHora,
                x.Tipo switch
                {
                    TipoActividadProducto.Desactivacion => "Producto desactivado",
                    TipoActividadProducto.Reactivacion => "Producto reactivado",
                    _ => "Actividad registrada"
                },
                x.Descripcion,
                x.UsuarioNombre)))
            .OrderByDescending(x => x.FechaHora)
            .ToList();
    }
}
