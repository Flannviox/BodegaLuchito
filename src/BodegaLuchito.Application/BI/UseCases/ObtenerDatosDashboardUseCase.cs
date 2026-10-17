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

    public async Task<DashboardResult> EjecutarAsync()
    {
        var hoy = DateTime.Today;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);
        var ayer = hoy.AddDays(-1);
        var inicioMesAnterior = inicioMes.AddMonths(-1);
        var finMesAnterior = inicioMes.AddDays(-1);
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

        decimal diferenciaCaja = 0m;

        return new DashboardResult(
            resumenHoy,
            resumenMes,
            mediosDePago,
            evolucionCompleta,
            productosVendidos,
            productosStockBajo,
            diferenciaCaja);
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
