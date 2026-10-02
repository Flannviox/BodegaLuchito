using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Domain.Ventas.Entities;

public sealed class DetalleVenta
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public Venta Venta { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; } 
    public decimal Subtotal { get; set; }
}
