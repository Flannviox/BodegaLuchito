using System.Collections.Generic;
using BodegaLuchito.Application.BI.DTOs;
using BodegaLuchito.Application.Inventario.DTOs;

namespace BodegaLuchito.Application.BI.UseCases;

public record DashboardResult(
    ResumenVentasDto VentasDelDia,
    ResumenVentasDto VentasDelMes,
    IReadOnlyList<ResumenMedioPagoDto> MediosDePagoDelMes,
    IReadOnlyList<VentaDiariaDto> EvolucionVentasUltimos7Dias,
    IReadOnlyList<ProductoMasVendidoDto> ProductosMasVendidosDelMes,
    IReadOnlyList<ExistenciaInventario> ProductosConStockBajo,
    decimal DiferenciaCajaDelDia)
{
    public DateTime InicioPeriodo { get; init; }
    public DateTime FinPeriodo { get; init; }
    public IReadOnlyList<VentaDiariaDto> EvolucionVentasDelMes { get; init; } = [];
    public IReadOnlyList<VentaCategoriaDto> VentasPorCategoria { get; init; } = [];
}
