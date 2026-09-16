using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Application.Productos.Interfaces;

public interface IProductoRepository
{
    Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default);
    Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default);
    Task<IEnumerable<Producto>> ObtenerActivosAsync(CancellationToken cancellationToken = default);

    Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default);
    Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default);
}
