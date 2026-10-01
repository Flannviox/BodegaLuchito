using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Abastecimiento.Interfaces;
using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Inventario.Entities;
using BodegaLuchito.Domain.Inventario.Enums;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Infrastructure.Abastecimiento.Repositories;

public sealed class AbastecimientoRepository : IAbastecimientoRepository
{
    private readonly IDbContextFactory<BodegaLuchitoDbContext> _contextFactory;

    public AbastecimientoRepository(IDbContextFactory<BodegaLuchitoDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task RegistrarOperacionAsync(
        EntidadAbastecimiento abastecimiento,
        int usuarioId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var proveedor = await context.Set<Proveedor>()
            .SingleOrDefaultAsync(x => x.Id == abastecimiento.ProveedorId, cancellationToken);

        if (proveedor is null || !proveedor.Activo)
            throw new InvalidOperationException("El proveedor seleccionado no existe o está inactivo.");

        var usuario = await context.Set<Usuario>()
            .SingleOrDefaultAsync(x => x.Id == usuarioId, cancellationToken);

        if (usuario is null || !usuario.Activo)
            throw new InvalidOperationException("El usuario que registra el abastecimiento no existe o está inactivo.");

        var sesionAbierta = await context.Set<SesionCaja>()
            .SingleOrDefaultAsync(x => x.Estado == EstadoSesionCaja.Abierta, cancellationToken);

        if (sesionAbierta is null)
            throw new InvalidOperationException("Debe abrir una sesión de caja antes de registrar un abastecimiento.");

        var productoIds = abastecimiento.Detalles
            .Select(x => x.ProductoId)
            .Distinct()
            .ToList();

        var productos = await context.Set<Producto>()
            .Where(x => productoIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        if (productos.Count != productoIds.Count)
            throw new InvalidOperationException("Uno de los productos seleccionados ya no existe.");

        if (productos.Values.Any(x => !x.Activo))
            throw new InvalidOperationException("No se puede registrar un abastecimiento con productos inactivos.");

        if (abastecimiento.Detalles.Any(detalle =>
                productos[detalle.ProductoId].UnidadVenta == UnidadVenta.Unidad &&
                detalle.Cantidad % 1 != 0))
        {
            throw new InvalidOperationException(
                "Los productos por unidad solo admiten cantidades enteras.");
        }

        await context.Set<EntidadAbastecimiento>().AddAsync(abastecimiento, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        foreach (var detalle in abastecimiento.Detalles)
        {
            var producto = productos[detalle.ProductoId];

            var stockAnterior = producto.StockActual;
            producto.StockActual += detalle.Cantidad;

            await context.Set<MovimientoInventario>().AddAsync(new MovimientoInventario
            {
                ProductoId = producto.Id,
                UsuarioId = usuarioId,
                Tipo = TipoMovimientoInventario.EntradaAbastecimiento,
                Cantidad = detalle.Cantidad,
                StockAnterior = stockAnterior,
                StockPosterior = producto.StockActual,
                FechaHora = abastecimiento.FechaHora,
                AbastecimientoId = abastecimiento.Id,
                Descripcion = $"Ingreso por abastecimiento #{abastecimiento.Id}"
            }, cancellationToken);
        }

        await context.Set<MovimientoCaja>().AddAsync(new MovimientoCaja
        {
            SesionCajaId = sesionAbierta.Id,
            UsuarioId = usuarioId,
            Tipo = TipoMovimientoCaja.EgresoAbastecimiento,
            MetodoPago = abastecimiento.MetodoPago,
            Monto = abastecimiento.Total,
            FechaHora = abastecimiento.FechaHora,
            AbastecimientoId = abastecimiento.Id,
            Descripcion = $"Pago por abastecimiento #{abastecimiento.Id}"
        }, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EntidadAbastecimiento>> ObtenerHistorialAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await context.Set<EntidadAbastecimiento>()
            .AsNoTracking()
            .Include(x => x.Proveedor)
            .Include(x => x.Detalles)
                .ThenInclude(x => x.Producto)
            .OrderByDescending(x => x.FechaHora)
            .ThenByDescending(x => x.Id)
            .Take(200)
            .ToListAsync(cancellationToken);
    }
}
