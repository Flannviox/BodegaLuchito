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

    public async Task<List<Proveedor>> ObtenerTodosAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<Proveedor>()
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<Proveedor?> ObtenerPorIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Set<Proveedor>().FindAsync(id);
    }

    public async Task ActualizarAsync(Proveedor proveedor)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Set<Proveedor>().Update(proveedor);
        await context.SaveChangesAsync();
    }

    public async Task EliminarAsync(Proveedor proveedor)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        context.Set<Proveedor>().Remove(proveedor);
        await context.SaveChangesAsync();
    }
}
