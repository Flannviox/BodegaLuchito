using BodegaLuchito.Application.Autenticacion.Interfaces;
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
    private readonly IUsuarioRepository _usuarioRepository;

    public ObtenerReportesUseCase(
        IVentaRepository ventaRepository,
        IAbastecimientoRepository abastecimientoRepository,
        ICajaRepository cajaRepository,
        IUsuarioRepository usuarioRepository)
    {
        _ventaRepository = ventaRepository;
        _abastecimientoRepository = abastecimientoRepository;
        _cajaRepository = cajaRepository;
        _usuarioRepository = usuarioRepository;
    }

    public async Task<ResultadoReporte> GenerarAsync(string tipo, DateTime inicio, DateTime fin)
    {
        ValidarPeriodo(inicio, fin);
        if (tipo is not ("Ventas" or "Abastecimientos" or "Movimientos de Caja"))
            throw new ArgumentException("Seleccione un tipo de reporte válido.");

        // Todas las tarjetas usan el mismo período, independientemente de la tabla elegida.
        var ventas = await ObtenerReporteVentasAsync(inicio, fin);
        var abastecimientos = await ObtenerReporteAbastecimientosAsync(inicio, fin);
        var cierres = await _cajaRepository.ObtenerCierresPorPeriodoAsync(inicio, fin);
        IReadOnlyList<object> filas = tipo switch
        {
            "Ventas" => ventas.Cast<object>().ToList(),
            "Abastecimientos" => abastecimientos.Cast<object>().ToList(),
            _ => (await ObtenerReporteCajaAsync(inicio, fin)).Cast<object>().ToList()
        };

        return new ResultadoReporte(tipo, inicio.Date, fin.Date, filas,
            ventas.Sum(v => v.Total), abastecimientos.Sum(a => a.Total),
            cierres.Sum(c => c.DiferenciaEfectivo ?? 0m), cierres.Count);
    }

    private static void ValidarPeriodo(DateTime inicio, DateTime fin)
    {
        if (inicio.Date > fin.Date)
            throw new ArgumentException("La fecha de inicio no puede ser posterior a la fecha final.");
        if (fin.Date == DateTime.MaxValue.Date)
            throw new ArgumentException("Seleccione una fecha final anterior al 31/12/9999.");
    }
    public async Task<IReadOnlyList<ReporteVentaDto>> ObtenerReporteVentasAsync(DateTime inicio, DateTime fin)
    {
        ValidarPeriodo(inicio, fin);
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
        ValidarPeriodo(inicio, fin);
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
        ValidarPeriodo(inicio, fin);
        var movimientos = await _cajaRepository.ObtenerMovimientosPorPeriodoAsync(inicio, fin);

        var usuarios = (await _usuarioRepository.ObtenerTodosAsync()).ToDictionary(u => u.Id, u => u.NombreCompleto);

        return movimientos.Select(m => new ReporteMovimientoCajaDto(
            m.FechaHora,
            m.Tipo.ToString(),
            m.Descripcion ?? "Operación de caja",
            m.MetodoPago.ToString(),
            m.Monto,
            usuarios.GetValueOrDefault(m.UsuarioId, "Usuario no disponible")
        )).ToList();
    }
}
