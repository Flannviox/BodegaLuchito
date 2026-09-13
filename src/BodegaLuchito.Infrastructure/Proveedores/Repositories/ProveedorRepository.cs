using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Proveedores.Repositories;

public class ProveedorRepository : IProveedorRepository
{
    private readonly IDbContextFactory<BodegaLuchitoDbContext> _contextFactory;

    public ProveedorRepository(IDbContextFactory<BodegaLuchitoDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task RegistrarAsync(Proveedor proveedor)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Set<Proveedor>().Add(proveedor);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExisteRucAsync(string ruc)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<Proveedor>().AnyAsync(p => p.Ruc == ruc);
    }

    public async Task<List<Proveedor>> ListarActivosAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<Proveedor>()
            .Where(p => p.Activo)
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }
}
