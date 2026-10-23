using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BodegaLuchito.Application.BI.DTOs;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Application.Inventario.Interfaces;
using BodegaLuchito.Application.Ventas.Interfaces;

namespace BodegaLuchito.Application.BI.UseCases;

public class ObtenerDatosDashboardUseCase
{
    private readonly IVentaRepository _ventaRepository;
    private readonly IInventarioRepository _inventarioRepository;
    private readonly ICajaRepository _cajaRepository;

    public ObtenerDatosDashboardUseCase(
        IVentaRepository ventaRepository,
        IInventarioRepository inventarioRepository,
        ICajaRepository cajaRepository)
    {
        _ventaRepository = ventaRepository;
        _inventarioRepository = inventarioRepository;
        _cajaRepository = cajaRepository;
    }

    public Task<DashboardResult> EjecutarAsync() => EjecutarAsync(DateTime.Today);

    public async Task<DashboardResult> EjecutarAsync(DateTime mes)
    {
        var inicioMes = new DateTime(mes.Year, mes.Month, 1);
        var mesActual = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        if (inicioMes > mesActual || inicioMes == DateTime.MinValue.Date)
            throw new ArgumentException("Seleccione el mes actual o un mes anterior dentro del rango permitido.");
        var hoy = inicioMes == mesActual ? DateTime.Today : inicioMes.AddMonths(1).AddDays(-1);
        var ayer = hoy.AddDays(-1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);
        // Un mes en curso se compara con el mismo tramo del mes anterior.
        var finMesAnterior = inicioMes == mesActual
            ? inicioMesAnterior.AddDays(Math.Min(hoy.Day, DateTime.DaysInMonth(inicioMesAnterior.Year, inicioMesAnterior.Month)) - 1)
            : inicioMes.AddDays(-1);
        var hace7Dias = hoy.AddDays(-6);
        // 1. Consultas a la Base de Datos
        var ventasHoy = await _ventaRepository.ObtenerVentasPorPeriodoAsync(hoy, hoy);
        var ventasAyer = await _ventaRepository.ObtenerVentasPorPeriodoAsync(ayer, ayer);
        var ventasEsteMes = await _ventaRepository.ObtenerVentasPorPeriodoAsync(inicioMes, hoy);
        var ventasMesAnterior = await _ventaRepository.ObtenerVentasPorPeriodoAsync(inicioMesAnterior, finMesAnterior);
        var ventasUltimos7Dias = await _ventaRepository.ObtenerVentasPorPeriodoAsync(hace7Dias, hoy);

        var existencias = await _inventarioRepository.ObtenerExistenciasAsync();
        var productosStockBajo = existencias.Where(e => e.StockActual <= e.StockMinimo && e.Activo).ToList();

        // 2. Procesamiento de DTOs
        var resumenHoy = CalcularResumenVentas(ventasHoy, ventasAyer);
        var resumenMes = CalcularResumenVentas(ventasEsteMes, ventasMesAnterior);

        var totalMes = ventasEsteMes.Sum(v => v.Total);
        var mediosDePago = ventasEsteMes
            .GroupBy(v => v.MetodoPago)
            .Select(g => new ResumenMedioPagoDto(
                g.Key.ToString(),
                g.Sum(v => v.Total),
                g.Count(),
                totalMes > 0 ? (g.Sum(v => v.Total) / totalMes) * 100 : 0))
            .ToList();

        var evolucion = ventasUltimos7Dias
            .GroupBy(v => v.FechaHora.Date)
            .Select(g => new VentaDiariaDto(g.Key, g.Sum(v => v.Total)))
            .OrderBy(v => v.Fecha)
            .ToList();

        var evolucionCompleta = new List<VentaDiariaDto>();
        for (int i = 0; i < 7; i++)
        {
            var fecha = hace7Dias.AddDays(i);
            var ventaDia = evolucion.FirstOrDefault(e => e.Fecha.Date == fecha.Date);
            evolucionCompleta.Add(ventaDia ?? new VentaDiariaDto(fecha, 0m));
        }

       
        var productosVendidos = ventasEsteMes
            .Where(v => v.Detalles != null)
            .SelectMany(v => v.Detalles)
            .GroupBy(d => new {
                d.ProductoId,
                ProductoNombre = d.Producto != null ? d.Producto.Nombre : $"Producto ID: {d.ProductoId}",
                CategoriaNombre = d.Producto != null && d.Producto.Categoria != null ? d.Producto.Categoria.Nombre : "Sin Categoría"
            })
            .Select(g => new ProductoMasVendidoDto(
                g.Key.ProductoId,
                g.Key.ProductoNombre,
                g.Key.CategoriaNombre,
                g.Sum(d => d.Cantidad),
                g.Sum(d => d.Subtotal)))
            .OrderByDescending(p => p.MontoTotalGenerado)
            .Take(5)
            .ToList();

        var cierresHoy = await _cajaRepository.ObtenerCierresPorPeriodoAsync(inicioMes, hoy);
        var diferenciaCaja = cierresHoy.Sum(c => c.DiferenciaEfectivo ?? 0m);

        var ventasPorDia = ventasEsteMes.GroupBy(v => v.FechaHora.Date)
            .ToDictionary(g => g.Key, g => g.Sum(v => v.Total));
        var evolucionMes = Enumerable.Range(0, (hoy - inicioMes).Days + 1)
            .Select(i => inicioMes.AddDays(i))
            .Select(fecha => new VentaDiariaDto(fecha, ventasPorDia.GetValueOrDefault(fecha)))
            .ToList();
        var categorias = ventasEsteMes.SelectMany(v => v.Detalles)
            .GroupBy(d => d.Producto?.Categoria?.Nombre ?? "Sin categoría")
            .Select(g => new VentaCategoriaDto(g.Key, g.Sum(d => d.Subtotal)))
            .OrderByDescending(c => c.TotalMonto).ToList();

        return new DashboardResult(
            resumenHoy,
            resumenMes,
            mediosDePago,
            evolucionCompleta,
            productosVendidos,
            productosStockBajo,
            diferenciaCaja)
        {
            InicioPeriodo = inicioMes,
            FinPeriodo = hoy,
            EvolucionVentasDelMes = evolucionMes,
            VentasPorCategoria = categorias
        };
    }

    private ResumenVentasDto CalcularResumenVentas(IEnumerable<BodegaLuchito.Domain.Ventas.Entities.Venta> actual, IEnumerable<BodegaLuchito.Domain.Ventas.Entities.Venta> anterior)
    {
        var totalActual = actual.Sum(v => v.Total);
        var totalAnterior = anterior.Sum(v => v.Total);
        var qtyActual = actual.Count();
        var qtyAnterior = anterior.Count();

        decimal varMonto = totalAnterior > 0 ? ((totalActual - totalAnterior) / totalAnterior) * 100 : 0;
        decimal varQty = qtyAnterior > 0 ? ((decimal)(qtyActual - qtyAnterior) / qtyAnterior) * 100 : 0;

        return new ResumenVentasDto(totalActual, qtyActual, Math.Round(varMonto, 2), Math.Round(varQty, 2));
    }
}
