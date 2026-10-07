using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Application.Caja.DTOs
{
    public sealed record MovimientoCajaDetalleResult(
    int MovimientoCajaId,
    DateTime FechaHora,
    TipoMovimientoCaja Tipo,
    MetodoPago MetodoPago,
    decimal Monto,
    int UsuarioId,
    int? VentaId,
    int? AbastecimientoId,
    string? Descripcion,
    IReadOnlyList<DetalleOperacionCajaItem> Detalles);
}
