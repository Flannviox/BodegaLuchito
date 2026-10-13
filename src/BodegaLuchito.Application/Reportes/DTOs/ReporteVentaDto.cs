using System;

namespace BodegaLuchito.Application.Reportes.DTOs;

public record ReporteVentaDto(
    DateTime Fecha,
    string Comprobante,
    string Cliente,
    int CantidadProductos,
    decimal TotalBruto,
    decimal Descuento,
    decimal Igv,
    decimal Total,
    string MedioPago,
    string Usuario);
