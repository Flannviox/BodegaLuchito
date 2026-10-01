using BodegaLuchito.Domain.Inventario.Enums;
using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Domain.Inventario.Entities;

public sealed class MovimientoInventario
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }
    public int UsuarioId { get; set; }
    public TipoMovimientoInventario Tipo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal StockAnterior { get; set; }
    public decimal StockPosterior { get; set; }
    public DateTime FechaHora { get; set; }
    public int? AbastecimientoId { get; set; }
    public string? Descripcion { get; set; }
}
