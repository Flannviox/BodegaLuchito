using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Application.Productos.UseCases;

public sealed class ConsultarHistorialGeneralProductosUseCase
{
    private readonly IProductoRepository _productoRepository;

    public ConsultarHistorialGeneralProductosUseCase(IProductoRepository productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task<IReadOnlyList<HistorialProductoGeneralItem>> EjecutarAsync(
        CancellationToken cancellationToken = default)
    {
        var historialPrecios = await _productoRepository.ObtenerHistorialPreciosGlobalAsync(cancellationToken);
        var historialActividad = await _productoRepository.ObtenerHistorialActividadGlobalAsync(cancellationToken);

        return historialPrecios
            .Select(x => new HistorialProductoGeneralItem(
                x.FechaHora,
                x.Producto?.Nombre ?? "Producto no disponible",
                "Precio actualizado",
                $"Precio de venta: S/ {x.PrecioAnterior:F2} → S/ {x.PrecioNuevo:F2}",
                x.UsuarioNombre))
            .Concat(historialActividad.Select(x => new HistorialProductoGeneralItem(
                x.FechaHora,
                x.Producto?.Nombre ?? "Producto no disponible",
                x.Tipo == TipoActividadProducto.Desactivacion
                    ? "Producto desactivado"
                    : "Producto reactivado",
                x.Descripcion,
                x.UsuarioNombre)))
            .OrderByDescending(x => x.FechaHora)
            .Take(200)
            .ToList();
    }
}
