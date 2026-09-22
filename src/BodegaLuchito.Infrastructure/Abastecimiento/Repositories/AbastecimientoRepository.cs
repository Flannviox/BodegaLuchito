using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Abastecimiento.Interfaces;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Infrastructure.Abastecimiento.Repositories;

public class AbastecimientoRepository : IAbastecimientoRepository
{
    private readonly IDbContextFactory<BodegaLuchitoDbContext> _contextFactory;

    public AbastecimientoRepository(IDbContextFactory<BodegaLuchitoDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task RegistrarAsync(EntidadAbastecimiento abastecimiento, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        // Al agregar la cabecera, EF Core agregará automáticamente la lista de Detalles vinculada
        await context.Set<EntidadAbastecimiento>().AddAsync(abastecimiento, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task AgregarMovimientoAsync(MovimientoCaja movimiento, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Set<MovimientoCaja>().AddAsync(movimiento, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}
