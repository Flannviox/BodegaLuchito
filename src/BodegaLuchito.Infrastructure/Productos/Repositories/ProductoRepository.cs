using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Productos.Repositories;

public class ProductoRepository : IProductoRepository
{
    private readonly BodegaLuchitoDbContext _context;

    public ProductoRepository(BodegaLuchitoDbContext context)
    {
        _context = context;
    }

    public async Task AgregarAsync(Producto producto)
    {
        await _context.Set<Producto>().AddAsync(producto);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> ExisteCodigoBarrasAsync(string codigoBarras)
    {
        return await _context.Set<Producto>()
            .AnyAsync(p => p.CodigoBarras == codigoBarras);
    }

    public async Task<IEnumerable<Producto>> ObtenerActivosAsync()
    {
        return await _context.Set<Producto>()
            .Where(p => p.Activo)
            .ToListAsync();
    }
}
