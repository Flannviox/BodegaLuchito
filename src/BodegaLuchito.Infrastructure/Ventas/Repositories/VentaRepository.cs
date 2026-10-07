using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Ventas.Interfaces;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BodegaLuchito.Application.Inventario.UseCases;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;


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
        await using var transaccion = await context.Database.BeginTransactionAsync(cancellationToken);
        if (venta.Id != 0 || venta.Anulado)
            throw new InvalidOperationException("La venta ya fue registrada o está anulada.");
        if (!await context.Set<Usuario>().AnyAsync(x => x.Id == venta.UsuarioId && x.Activo, cancellationToken))
            throw new InvalidOperationException("El usuario de la venta no existe o está desactivado.");
        var sesionAbierta = await context.Set<SesionCaja>().SingleOrDefaultAsync(x => x.Estado ==
        EstadoSesionCaja.Abierta, cancellationToken);
        if (sesionAbierta is null){
            throw new InvalidOperationException("Debe abrir una sesión de caja antes de registrar una venta.");
        }
        var ids = venta.Detalles.Select(x => x.ProductoId).Distinct().ToList();
        var productos = await context.Productos.Where(x => ids.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var movimientos = PrepararSalidaVentaUseCase.Ejecutar(venta, productos);
        await context.Set<Venta>().AddAsync(venta, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        foreach (var movimiento in movimientos)
            movimiento.Descripcion = $"Salida por venta #{venta.Id}";
        context.MovimientosInventario.AddRange(movimientos);
        await context.SaveChangesAsync(cancellationToken);

        await context.Set<MovimientoCaja>().AddAsync(
         new MovimientoCaja
            {
                SesionCajaId = sesionAbierta.Id,
                UsuarioId = venta.UsuarioId,
                Tipo = TipoMovimientoCaja.IngresoVenta,
                MetodoPago = venta.MetodoPago,
                Monto = venta.Total,
                FechaHora = venta.FechaHora,
                VentaId = venta.Id,
                Descripcion = $"Ingreso por venta #{venta.Id}"
            },
          cancellationToken);
         await context.SaveChangesAsync(cancellationToken);
         await transaccion.CommitAsync(cancellationToken);
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
    public async Task<IReadOnlyList<Venta>> ObtenerHistorialVentasAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Set<Venta>()
            .Include(v => v.Usuario) // Para ver quién la hizo
            .Include(v => v.Detalles) // Para saber cuántos productos tiene
            .OrderByDescending(v => v.FechaHora)
            .Take(100) // Traemos las últimas 100 por rendimiento
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
