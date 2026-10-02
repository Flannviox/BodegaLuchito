using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Domain.Ventas.Entities;

namespace BodegaLuchito.Application.Ventas.Interfaces;

public interface IVentaRepository
{
    Task RegistrarVentaAsync(Venta venta, CancellationToken cancellationToken = default);
    Task<Venta?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task AnularVentaAsync(Venta venta, CancellationToken cancellationToken = default);
}
