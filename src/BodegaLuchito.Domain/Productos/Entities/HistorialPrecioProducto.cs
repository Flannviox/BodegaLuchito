namespace BodegaLuchito.Domain.Productos.Entities;

public sealed class HistorialPrecioProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public decimal PrecioAnterior { get; set; }
    public decimal PrecioNuevo { get; set; }

    public DateTime FechaHora { get; set; }

    public string UsuarioNombre { get; set; } = "Sistema";
}
