using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Ventas.Interfaces;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Ventas.Repositories;

public sealed class VentaRepository : IVentaRepository
{
    private readonly IDbContextFactory<BodegaLuchitoDbContext> _contextFactory;

    public VentaRepository(IDbContextFactory<BodegaLuchitoDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task RegistrarVentaAsync(Venta venta, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Set<Venta>().AddAsync(venta, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Venta?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Venta>()
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Producto)
            .Include(v => v.Usuario)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public async Task AnularVentaAsync(Venta venta, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Set<Venta>().Update(venta);
        await context.SaveChangesAsync(cancellationToken);
    }
}
