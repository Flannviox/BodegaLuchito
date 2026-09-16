using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Application.Productos.DTOs;

public record ModificarProductoRequest(
    int Id,
    string Nombre,
    string Categoria,
    string? CodigoBarras,
    decimal PrecioVenta,
    UnidadVenta UnidadVenta,
    bool ControlaInventario,
    decimal StockActual,
    decimal StockMinimo
);
