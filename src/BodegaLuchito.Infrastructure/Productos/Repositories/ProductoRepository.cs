using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Productos.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly IDbContextFactory<BodegaLuchitoDbContext> _contextFactory;

    public ProductoRepository(IDbContextFactory<BodegaLuchitoDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AgregarAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Set<Producto>().AddAsync(producto, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Producto>()
            .AsNoTracking()
            .AnyAsync(p => p.CodigoBarras == codigoBarras, cancellationToken);
    }

    public async Task<IEnumerable<Producto>> ObtenerActivosAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Producto>()
            .AsNoTracking()
            .Where(p => p.Activo)
            .ToListAsync(cancellationToken);
    }
}
