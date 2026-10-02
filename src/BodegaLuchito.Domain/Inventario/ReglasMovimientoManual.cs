using BodegaLuchito.Domain.Inventario.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
namespace BodegaLuchito.Domain.Inventario;

public static class ReglasMovimientoManual
{
    public static decimal CalcularStockPosterior(Producto producto, TipoMovimientoInventario tipo,
        decimal cantidad, decimal stockEsperado, bool tieneOperaciones, string motivo)
    {
        if (!producto.Activo)
            throw new InvalidOperationException("Reactiva el producto antes de registrar movimientos.");
        if (producto.StockActual != stockEsperado)
            throw new InvalidOperationException("El stock cambió. Actualiza la lista y vuelve a revisar el movimiento.");
        if (tipo is not (TipoMovimientoInventario.SaldoInicial or TipoMovimientoInventario.AjusteConteo or TipoMovimientoInventario.Merma))
            throw new ArgumentException("Selecciona una operación de inventario válida.");
        if (cantidad < 0 || cantidad > 999999999m || decimal.Round(cantidad, 3) != cantidad)
            throw new ArgumentException("Ingresa una cantidad válida, con un máximo de tres decimales.");
        if (producto.UnidadVenta == UnidadVenta.Unidad && cantidad % 1 != 0)
            throw new ArgumentException("Los productos por unidad requieren cantidades enteras.");
        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 250)
            throw new ArgumentException("Escribe un motivo de hasta 250 caracteres.");

        if (tipo == TipoMovimientoInventario.SaldoInicial)
        {
            if (producto.StockActual != 0 || tieneOperaciones)
                throw new InvalidOperationException("El saldo inicial solo se registra una vez, antes de cualquier movimiento.");
            if (cantidad == 0)
                throw new ArgumentException("El saldo inicial debe ser mayor que cero.");
            return cantidad;
        }
        if (tipo == TipoMovimientoInventario.Merma)
        {
            if (cantidad <= 0 || cantidad > producto.StockActual)
                throw new ArgumentException("La pérdida debe ser mayor que cero y no superar el stock disponible.");
            return producto.StockActual - cantidad;
        }
        if (cantidad == producto.StockActual)
            throw new ArgumentException("El conteo coincide con el stock actual; no hay diferencia que registrar.");
        return cantidad;
    }
}
