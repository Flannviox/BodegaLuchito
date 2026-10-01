using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Inventario.Interfaces;
using BodegaLuchito.Domain.Inventario.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Inventario.Repositories;

public sealed class InventarioRepository : IInventarioRepository
{
    private readonly IDbContextFactory<BodegaLuchitoDbContext> _contextFactory;

    public InventarioRepository(IDbContextFactory<BodegaLuchitoDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<IReadOnlyList<MovimientoInventario>> ObtenerMovimientosAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<MovimientoInventario>()
            .AsNoTracking()
            .Include(x => x.Producto)
            .OrderByDescending(x => x.FechaHora)
            .ThenByDescending(x => x.Id)
            .Take(200)
            .ToListAsync(cancellationToken);
    }
}
