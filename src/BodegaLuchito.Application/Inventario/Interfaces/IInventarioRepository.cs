using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Domain.Inventario.Entities;

namespace BodegaLuchito.Application.Inventario.Interfaces;

public interface IInventarioRepository
{
    Task<IReadOnlyList<MovimientoInventario>> ObtenerMovimientosAsync(
        CancellationToken cancellationToken = default);
}
