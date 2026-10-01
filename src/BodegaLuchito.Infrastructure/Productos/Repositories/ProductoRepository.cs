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
            .Include(p => p.Categoria) // <-- Agregamos el Include para cargar la categoría relacionada
            .AsNoTracking()
            .Where(p => p.Activo)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Producto>> ObtenerInactivosAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Producto>()
            .Include(p => p.Categoria)
            .AsNoTracking()
            .Where(p => !p.Activo)
            .ToListAsync(cancellationToken);
    }

    public async Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Producto>()
            .Include(p => p.Categoria) 
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task ActualizarAsync(Producto producto, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Set<Producto>().Update(producto);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task ActualizarConHistorialPrecioAsync(
        Producto producto,
        HistorialPrecioProducto? historialPrecio,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        context.Set<Producto>().Update(producto);

        if (historialPrecio is not null)
            await context.Set<HistorialPrecioProducto>().AddAsync(historialPrecio, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistorialPrecioProducto>> ObtenerHistorialPreciosAsync(
        int productoId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<HistorialPrecioProducto>()
            .AsNoTracking()
            .Where(x => x.ProductoId == productoId)
            .OrderByDescending(x => x.FechaHora)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistorialPrecioProducto>> ObtenerHistorialPreciosGlobalAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<HistorialPrecioProducto>()
            .AsNoTracking()
            .Include(x => x.Producto)
            .OrderByDescending(x => x.FechaHora)
            .Take(200)
            .ToListAsync(cancellationToken);
    }

    public async Task ActualizarEstadoConHistorialAsync(
        Producto producto,
        HistorialActividadProducto historialActividad,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        context.Set<Producto>().Update(producto);
        await context.Set<HistorialActividadProducto>().AddAsync(historialActividad, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistorialActividadProducto>> ObtenerHistorialActividadAsync(
        int productoId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<HistorialActividadProducto>()
            .AsNoTracking()
            .Where(x => x.ProductoId == productoId)
            .OrderByDescending(x => x.FechaHora)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<HistorialActividadProducto>> ObtenerHistorialActividadGlobalAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<HistorialActividadProducto>()
            .AsNoTracking()
            .Include(x => x.Producto)
            .OrderByDescending(x => x.FechaHora)
            .Take(200)
            .ToListAsync(cancellationToken);
    }
}
