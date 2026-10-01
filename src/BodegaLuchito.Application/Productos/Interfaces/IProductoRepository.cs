using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Application.Productos.Interfaces;

public interface IProductoRepository
{
    Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default);
    Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default);
    Task<IEnumerable<Producto>> ObtenerActivosAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Producto>> ObtenerInactivosAsync(CancellationToken cancellationToken = default);

    Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default);
    Task ActualizarConHistorialPrecioAsync(
        Producto producto,
        HistorialPrecioProducto? historialPrecio,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistorialPrecioProducto>> ObtenerHistorialPreciosAsync(
        int productoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistorialPrecioProducto>> ObtenerHistorialPreciosGlobalAsync(
        CancellationToken cancellationToken = default);
    Task ActualizarEstadoConHistorialAsync(
        Producto producto,
        HistorialActividadProducto historialActividad,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistorialActividadProducto>> ObtenerHistorialActividadAsync(
        int productoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HistorialActividadProducto>> ObtenerHistorialActividadGlobalAsync(
        CancellationToken cancellationToken = default);
}
