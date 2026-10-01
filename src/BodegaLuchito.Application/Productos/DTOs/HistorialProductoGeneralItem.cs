namespace BodegaLuchito.Application.Productos.DTOs;

public sealed record HistorialProductoGeneralItem(
    DateTime FechaHora,
    string Producto,
    string Accion,
    string Detalle,
    string UsuarioNombre);
