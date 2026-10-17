namespace BodegaLuchito.Application.BI.DTOs;

public record ProductoMasVendidoDto(
    int ProductoId,
    string NombreProducto,
    string Categoria,
    decimal CantidadVendida,
    decimal MontoTotalGenerado);
