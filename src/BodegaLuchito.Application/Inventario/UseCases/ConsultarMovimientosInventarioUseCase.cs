using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Inventario.Interfaces;
using BodegaLuchito.Domain.Inventario.Entities;

namespace BodegaLuchito.Application.Inventario.UseCases;

public sealed class ConsultarMovimientosInventarioUseCase
{
    private readonly IInventarioRepository _inventarioRepository;

    public ConsultarMovimientosInventarioUseCase(IInventarioRepository inventarioRepository)
    {
        _inventarioRepository = inventarioRepository;
    }

    public Task<IReadOnlyList<MovimientoInventario>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        return _inventarioRepository.ObtenerMovimientosAsync(cancellationToken);
    }
}
