using System;

namespace BodegaLuchito.Domain.Productos.Entities;

public sealed class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}
