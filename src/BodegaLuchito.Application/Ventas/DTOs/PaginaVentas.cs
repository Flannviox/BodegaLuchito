using BodegaLuchito.Domain.Ventas.Entities;

namespace BodegaLuchito.Application.Ventas.DTOs;

/// <summary>Una página del historial de ventas, con el total de ventas que cumplen el filtro.</summary>
public sealed record PaginaVentas(IReadOnlyList<Venta> Ventas, int TotalVentas, int Pagina, int TamanoPagina)
{
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling(TotalVentas / (double)TamanoPagina));
}
