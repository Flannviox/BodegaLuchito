using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Productos.Repositories;

public sealed class CategoriaRepository : ICategoriaRepository
{
    private readonly IDbContextFactory<BodegaLuchitoDbContext> _contextFactory;

    public CategoriaRepository(IDbContextFactory<BodegaLuchitoDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task AgregarAsync(Categoria categoria, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Set<Categoria>().AddAsync(categoria, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        // Validamos ignorando mayúsculas y minúsculas en SQLite
        return await context.Set<Categoria>()
            .AsNoTracking()
            .AnyAsync(c => c.Nombre.ToLower() == nombre.ToLower() && c.Activo, cancellationToken);
    }

    public async Task<IEnumerable<Categoria>> ObtenerActivasAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Categoria>()
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre) // Las ordenamos alfabéticamente para que se vean bien en el ComboBox
            .ToListAsync(cancellationToken);
    }
}
