using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Domain.Ventas.Entities;

namespace BodegaLuchito.Application.Ventas.Interfaces;

public interface IVentaRepository
{
    Task<IReadOnlyList<Venta>> ObtenerHistorialVentasAsync(CancellationToken cancellationToken = default);
    Task RegistrarVentaAsync(Venta venta, CancellationToken cancellationToken = default);
    Task<Venta?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task AnularVentaAsync(Venta venta, CancellationToken cancellationToken = default);
}

