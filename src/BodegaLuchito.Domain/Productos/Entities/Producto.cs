using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Domain.Productos.Entities;

public sealed class Producto
{
    public int Id { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public int CategoriaId { get; set; }
    public Categoria Categoria { get; set; } = null!;

    public string? CodigoBarras { get; set; }

    public decimal PrecioVenta { get; set; }

    public UnidadVenta UnidadVenta { get; set; }

    public bool ControlaInventario { get; set; }

    public decimal StockActual { get; set; }

    public decimal StockMinimo { get; set; }

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } =
        DateTime.Now;
}
