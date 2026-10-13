using System;

namespace BodegaLuchito.Application.Reportes.DTOs;

public record ReporteMovimientoCajaDto(
    DateTime Fecha,
    string TipoMovimiento,
    string Descripcion,
    string MedioPago,
    decimal Monto,
    string Usuario);
