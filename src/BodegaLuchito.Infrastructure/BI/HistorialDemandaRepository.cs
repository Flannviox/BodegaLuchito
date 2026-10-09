using BodegaLuchito.Application.BI;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.BI;

public sealed class HistorialDemandaRepository(IDbContextFactory<BodegaLuchitoDbContext> factory)
    : IHistorialDemandaRepository
{
    public async Task<IReadOnlyList<HistorialDemanda>> ConsultarAsync(DateTime desde,
        DateTime hastaExclusiva, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        await using var lectura = await db.Database.BeginTransactionAsync(cancellationToken);
        var productos = await db.Productos.AsNoTracking().Where(p => p.Activo && p.ControlaInventario)
            .OrderBy(p => p.Nombre).ToListAsync(cancellationToken);
        var primeraVenta = await db.Ventas.AsNoTracking().Where(v => !v.Anulado && v.FechaHora < hastaExclusiva)
            .MinAsync(v => (DateTime?)v.FechaHora, cancellationToken);
        var detalles = await db.DetallesVenta.AsNoTracking()
            .Where(d => !d.Venta.Anulado && d.Venta.FechaHora >= desde && d.Venta.FechaHora < hastaExclusiva
                && d.Producto.Activo && d.Producto.ControlaInventario)
            .Select(d => new { d.ProductoId, d.Venta.FechaHora, d.Cantidad }).ToListAsync(cancellationToken);
        var diarios = detalles.GroupBy(d => d.ProductoId).ToDictionary(g => g.Key,
            g => g.GroupBy(d => d.FechaHora.Date).Select(d => new VentaDiaria(d.Key, d.Sum(x => x.Cantidad))).ToArray());
        // No inventar días anteriores al primer registro del negocio ni al alta del producto.
        // Se omite el día de alta (podría ser parcial), igual que el día actual.
        return productos.Select(p => new HistorialDemanda(p.Id, p.Nombre,
            p.UnidadVenta == UnidadVenta.Peso ? "kg" : "unid", p.StockActual,
            new[] { desde.Date, p.FechaCreacion.Date.AddDays(1), primeraVenta?.Date ?? hastaExclusiva }.Max(),
            diarios.GetValueOrDefault(p.Id) ?? [])).ToArray();
    }
}
