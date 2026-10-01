using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Abastecimiento.Interfaces;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Application.Abastecimiento.UseCases;

public sealed class ConsultarHistorialAbastecimientosUseCase
{
    private readonly IAbastecimientoRepository _abastecimientoRepository;

    public ConsultarHistorialAbastecimientosUseCase(
        IAbastecimientoRepository abastecimientoRepository)
    {
        _abastecimientoRepository = abastecimientoRepository;
    }

    public Task<IReadOnlyList<EntidadAbastecimiento>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        return _abastecimientoRepository.ObtenerHistorialAsync(cancellationToken);
    }
}
