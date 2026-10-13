using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Abastecimiento.Interfaces;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Application.Ventas.Interfaces;
using BodegaLuchito.Application.Reportes.DTOs;

namespace BodegaLuchito.Application.Reportes.UseCases;

public class ObtenerReportesUseCase
{
    private readonly IVentaRepository _ventaRepository;
    private readonly IAbastecimientoRepository _abastecimientoRepository;
    private readonly ICajaRepository _cajaRepository;

    public ObtenerReportesUseCase(
        IVentaRepository ventaRepository,
        IAbastecimientoRepository abastecimientoRepository,
        ICajaRepository cajaRepository)
    {
        _ventaRepository = ventaRepository;
        _abastecimientoRepository = abastecimientoRepository;
        _cajaRepository = cajaRepository;
    }

    public async Task<IReadOnlyList<ReporteVentaDto>> ObtenerReporteVentasAsync(DateTime inicio, DateTime fin)
    {
        var ventas = await _ventaRepository.ObtenerVentasPorPeriodoAsync(inicio, fin);

        return ventas.Select(v => new ReporteVentaDto(
            v.FechaHora,
            $"Venta #{v.Id}",
            "Cliente General",
            v.Detalles?.Count ?? 0,
            v.Subtotal,
            0m, // Descuento
            v.IGV,
            v.Total,
            v.MetodoPago.ToString(),
            v.Usuario?.NombreCompleto ?? "Usuario"
        )).ToList();
    }

    public async Task<IReadOnlyList<ReporteAbastecimientoDto>> ObtenerReporteAbastecimientosAsync(DateTime inicio, DateTime fin)
    {
        var abastecimientos = await _abastecimientoRepository.ObtenerAbastecimientosPorPeriodoAsync(inicio, fin);

        return abastecimientos.Select(a => {
            // Calculamos matemáticamente el IGV y Subtotal a partir del Total guardado
            decimal subtotalCalc = Math.Round(a.Total / 1.18m, 2);
            decimal igvCalc = a.Total - subtotalCalc;

            return new ReporteAbastecimientoDto(
                a.FechaHora,
                a.Proveedor?.Nombre ?? "Proveedor Genérico",
                a.Detalles?.Count ?? 0,
                a.Detalles?.Sum(d => d.Cantidad) ?? 0,
                subtotalCalc, // Se usa el cálculo
                igvCalc,      // Se usa el cálculo
                a.Total,
                a.MetodoPago.ToString()
            );
        }).ToList();
    }

    public async Task<IReadOnlyList<ReporteMovimientoCajaDto>> ObtenerReporteCajaAsync(DateTime inicio, DateTime fin)
    {
        var movimientos = await _cajaRepository.ObtenerMovimientosPorPeriodoAsync(inicio, fin);

        return movimientos.Select(m => new ReporteMovimientoCajaDto(
            m.FechaHora,
            m.Tipo.ToString(),
            m.Descripcion ?? "Operación de caja",
            m.MetodoPago.ToString(),
            m.Monto,
            "Usuario ID: " + m.UsuarioId
        )).ToList();
    }
}
