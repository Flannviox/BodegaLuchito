using System.Threading;
using System.Threading.Tasks;
// Usamos un alias para evitar el choque de nombres con la carpeta
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Application.Abastecimiento.Interfaces;

public interface IAbastecimientoRepository
{
    Task RegistrarAsync(EntidadAbastecimiento abastecimiento, CancellationToken cancellationToken = default);
}
