using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Proveedores.Repositories;

public class ProveedorRepository : IProveedorRepository
{
    private readonly BodegaLuchitoDbContext _context;

    public ProveedorRepository(BodegaLuchitoDbContext context)
    {
        _context = context;
    }

    public async Task RegistrarAsync(Proveedor proveedor)
    {
        _context.Set<Proveedor>().Add(proveedor);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExisteRucAsync(string ruc)
    {
        return await _context.Set<Proveedor>().AnyAsync(p => p.Ruc == ruc);
    }

    public async Task<List<Proveedor>> ListarActivosAsync()
    {
        return await _context.Set<Proveedor>()
            .Where(p => p.Activo)
            .OrderByDescending(p => p.Id)
            .ToListAsync();
    }
}
