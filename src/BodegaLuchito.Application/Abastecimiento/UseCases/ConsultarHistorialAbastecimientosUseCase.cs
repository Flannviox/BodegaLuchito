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
        CancellationToken cancellationToken = default, DateTime? desde = null, DateTime? hasta = null, int? proveedorId = null)
    {
        if (desde.HasValue && hasta.HasValue && desde.Value.Date > hasta.Value.Date)
            throw new ArgumentException("La fecha Desde no puede ser posterior a Hasta.");
        if (hasta?.Date == DateTime.MaxValue.Date)
            throw new ArgumentException("Selecciona una fecha final anterior al 31/12/9999.");
        if (proveedorId <= 0) throw new ArgumentException("Selecciona un proveedor válido.");
        return _abastecimientoRepository.ObtenerHistorialAsync(cancellationToken, desde, hasta, proveedorId);
    }
}
