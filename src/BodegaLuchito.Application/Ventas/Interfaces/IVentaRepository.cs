using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Ventas.DTOs;
using BodegaLuchito.Domain.Ventas.Entities;

namespace BodegaLuchito.Application.Ventas.Interfaces;

public interface IVentaRepository
{
    Task<IReadOnlyList<Venta>> ObtenerHistorialVentasAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Historial paginado, de la venta más reciente a la más antigua.
    /// Las fechas son opcionales e inclusivas (se consideran días completos).
    /// Si la página pedida no existe, se devuelve la última disponible.
    /// </summary>
    Task<PaginaVentas> ObtenerHistorialPaginadoAsync(DateTime? desde, DateTime? hasta, int pagina, int tamanoPagina,
        CancellationToken cancellationToken = default);
    Task RegistrarVentaAsync(Venta venta, CancellationToken cancellationToken = default);
    Task<Venta?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task AnularVentaAsync(Venta venta, int sesionCajaAbiertaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Venta>> ObtenerVentasPorPeriodoAsync(DateTime inicio, DateTime fin, CancellationToken cancellationToken = default);
}
