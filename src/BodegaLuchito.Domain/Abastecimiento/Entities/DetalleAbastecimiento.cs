using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Domain.Abastecimiento.Entities;

public class DetalleAbastecimiento
{
    public int Id { get; set; }

    public int AbastecimientoId { get; set; }
    public Abastecimiento Abastecimiento { get; set; } = null!;

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal TotalLinea { get; set; }
}
