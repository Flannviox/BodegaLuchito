using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Domain.Inventario.Entities;
using BodegaLuchito.Application.Inventario.DTOs;
using BodegaLuchito.Domain.Inventario.Enums;

namespace BodegaLuchito.Application.Inventario.Interfaces;

public interface IInventarioRepository
{
    Task<IReadOnlyList<ExistenciaInventario>> ObtenerExistenciasAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoInventarioDetalle>> ObtenerHistorialCompletoAsync(
        CancellationToken cancellationToken = default);
    Task RegistrarMovimientoManualAsync(
        RegistrarMovimientoManualRequest solicitud,
        int usuarioId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MovimientoInventario>> ObtenerMovimientosAsync(
        CancellationToken cancellationToken = default, TipoMovimientoInventario? tipo = null);
}
