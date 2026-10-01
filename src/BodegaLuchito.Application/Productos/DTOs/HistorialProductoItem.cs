namespace BodegaLuchito.Application.Productos.DTOs;

public sealed record HistorialProductoItem(
    DateTime FechaHora,
    string Accion,
    string Detalle,
    string UsuarioNombre);
