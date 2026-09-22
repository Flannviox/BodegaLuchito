using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

        public Task GuardarCambiosAsync(CancellationToken cancellationToken = default)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }
        public async Task AgregarMovimientoAsync(MovimientoCaja movimiento, CancellationToken cancellationToken = default)
        {
            await _context.Set<MovimientoCaja>().AddAsync(movimiento, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

    }
}
