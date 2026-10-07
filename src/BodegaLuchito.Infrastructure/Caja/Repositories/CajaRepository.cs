using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Domain.Ventas.Entities;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Infrastructure.Caja.Repositories
{
    public class CajaRepository : ICajaRepository
    {
        private readonly BodegaLuchitoDbContext _context;

        public CajaRepository(BodegaLuchitoDbContext context)
        {
            _context = context;
        }

        public Task<SesionCaja?> ObtenerSesionAbiertaAsync(CancellationToken cancellationToken = default)
        {
            return _context.Set<SesionCaja>()
                .FirstOrDefaultAsync(s => s.Estado == EstadoSesionCaja.Abierta, cancellationToken);

        }


        public async Task AgregarSesionAsync(SesionCaja sesion, CancellationToken cancellationToken = default)
        {
            await _context.Set<SesionCaja>().AddAsync(sesion, cancellationToken);
        }

        public async Task<IReadOnlyList<MovimientoCaja>> ObtenerMovimientosPorSesionAsync(
            int sesionCajaId, CancellationToken cancellationToken = default)
        {
            var movimientos = await _context
                    .Set<MovimientoCaja>()
                    .Where(m => m.SesionCajaId == sesionCajaId)
                    .ToListAsync(cancellationToken);

            return movimientos;
        }

        public async Task<IReadOnlyList<MovimientoCajaDetalleResult>>
        ObtenerDetalleMovimientosPorSesionAsync(
            int sesionCajaId,
            CancellationToken cancellationToken = default)
            {
                var movimientos = await _context
                    .Set<MovimientoCaja>()
                    .AsNoTracking()
                    .Where(m => m.SesionCajaId == sesionCajaId)
                    .OrderBy(m => m.FechaHora)
                    .ThenBy(m => m.Id)
                    .ToListAsync(cancellationToken);

                var ventaIds = movimientos
                    .Where(m => m.VentaId.HasValue)
                    .Select(m => m.VentaId!.Value)
                    .Distinct()
                    .ToList();

                var abastecimientoIds = movimientos
                    .Where(m => m.AbastecimientoId.HasValue)
                    .Select(m => m.AbastecimientoId!.Value)
                    .Distinct()
                    .ToList();

                var ventas = await _context
                    .Set<Venta>()
                    .AsNoTracking()
                    .Include(v => v.Detalles)
                        .ThenInclude(d => d.Producto)
                    .Where(v => ventaIds.Contains(v.Id))
                    .ToDictionaryAsync(v => v.Id, cancellationToken);

                var abastecimientos = await _context
                    .Set<EntidadAbastecimiento>()
                    .AsNoTracking()
                    .Include(a => a.Detalles)
                        .ThenInclude(d => d.Producto)
                    .Where(a => abastecimientoIds.Contains(a.Id))
                    .ToDictionaryAsync(a => a.Id, cancellationToken);

                return movimientos
                    .Select(movimiento =>
                    {
                        IReadOnlyList<DetalleOperacionCajaItem> detalles =
                            [];

                        if (movimiento.VentaId.HasValue &&
                            ventas.TryGetValue(
                                movimiento.VentaId.Value,
                                out var venta))
                        {
                            detalles = venta.Detalles
                                .Select(detalle =>
                                    new DetalleOperacionCajaItem(
                                        detalle.ProductoId,
                                        detalle.Producto.Nombre,
                                        detalle.Cantidad,
                                        detalle.PrecioUnitario,
                                        detalle.Subtotal))
                                .ToList();
                        }
                        else if (
                            movimiento.AbastecimientoId.HasValue &&
                            abastecimientos.TryGetValue(
                                movimiento.AbastecimientoId.Value,
                                out var abastecimiento))
                        {
                            detalles = abastecimiento.Detalles
                                .Select(detalle =>
                                    new DetalleOperacionCajaItem(
                                        detalle.ProductoId,
                                        detalle.Producto.Nombre,
                                        detalle.Cantidad,
                                        detalle.CostoUnitario,
                                        detalle.TotalLinea))
                                .ToList();
                        }

                        return new MovimientoCajaDetalleResult(
                            movimiento.Id,
                            movimiento.FechaHora,
                            movimiento.Tipo,
                            movimiento.MetodoPago,
                            movimiento.Monto,
                            movimiento.UsuarioId,
                            movimiento.VentaId,
                            movimiento.AbastecimientoId,
                            movimiento.Descripcion,
                            detalles);
                    })
                    .ToList();
            }

        public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
        public async Task AgregarMovimientoAsync(MovimientoCaja movimiento, CancellationToken cancellationToken = default)
        {
            await _context.Set<MovimientoCaja>().AddAsync(movimiento, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<SesionCaja>> ObtenerHistorialCierresAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.Set<SesionCaja>()
                .AsNoTracking()
                .Where(s => s.Estado == EstadoSesionCaja.Cerrada)
                .OrderByDescending(s => s.FechaCierre)
                .ThenByDescending(s => s.Id)
                .Take(200)
                .ToListAsync(cancellationToken);

        }

    }
}
