using System;

namespace BodegaLuchito.Application.Reportes.DTOs;

public record ReporteAbastecimientoDto(
    DateTime Fecha,
    string Proveedor,
    int CantidadProductos,
    decimal TotalUnidades,
    decimal Subtotal,
    decimal Igv,
    decimal Total,
    string MedioPago);
