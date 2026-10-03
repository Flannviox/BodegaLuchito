using BodegaLuchito.Domain.Inventario.Entities;
using BodegaLuchito.Domain.Inventario.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Ventas.Entities;

namespace BodegaLuchito.Application.Inventario.UseCases;

// La persistencia ejecuta este contrato dentro de la transacción de la venta.
public static class PrepararSalidaVentaUseCase
{
    public static IReadOnlyList<MovimientoInventario> Ejecutar(
        Venta venta, IReadOnlyDictionary<int, Producto> productos)
    {
        if (venta.Detalles.Count == 0) throw new ArgumentException("La venta no contiene productos.");
        foreach (var detalle in venta.Detalles)
        {
            if (!productos.TryGetValue(detalle.ProductoId, out var producto) || !producto.Activo)
                throw new InvalidOperationException("Un producto de la venta ya no existe o está desactivado.");
            if (detalle.Cantidad <= 0 || decimal.Round(detalle.Cantidad, 3) != detalle.Cantidad)
                throw new ArgumentException($"Revisa la cantidad de '{producto.Nombre}': admite hasta tres decimales.");
            if (producto.UnidadVenta == UnidadVenta.Unidad && detalle.Cantidad % 1 != 0)
                throw new ArgumentException($"'{producto.Nombre}' solo admite cantidades enteras.");
        }
        // Sumar líneas repetidas impide superar el stock dividiendo la cantidad.
        var cantidades = venta.Detalles.GroupBy(x => x.ProductoId)
            .ToDictionary(x => x.Key, x => x.Sum(d => d.Cantidad));
        foreach (var (id, cantidad) in cantidades)
        {
            var producto = productos[id];
            if (cantidad > producto.StockActual)
                throw new InvalidOperationException($"Stock insuficiente de '{producto.Nombre}'. Disponible: {producto.StockActual:0.###}; solicitado: {cantidad:0.###}.");
        }
        var movimientos = new List<MovimientoInventario>();
        foreach (var (id, cantidad) in cantidades)
        {
            var producto = productos[id];
            var anterior = producto.StockActual;
            producto.StockActual -= cantidad;
            movimientos.Add(new MovimientoInventario {
                ProductoId = id, UsuarioId = venta.UsuarioId, Tipo = TipoMovimientoInventario.Venta,
                Cantidad = cantidad, StockAnterior = anterior, StockPosterior = producto.StockActual,
                FechaHora = venta.FechaHora
            });
        }
        return movimientos;
    }
}
