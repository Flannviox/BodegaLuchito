using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Domain.Productos.Entities;

public sealed class HistorialActividadProducto
{
    public int Id { get; set; }

    public int ProductoId { get; set; }
    public Producto Producto { get; set; } = null!;

    public TipoActividadProducto Tipo { get; set; }
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaHora { get; set; }
    public string UsuarioNombre { get; set; } = "Sistema";
}
