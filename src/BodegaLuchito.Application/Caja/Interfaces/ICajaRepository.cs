using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Application.Caja.DTOs;

namespace BodegaLuchito.Application.Caja.Interfaces
{
    public interface ICajaRepository
    {
        Task<SesionCaja?> ObtenerSesionAbiertaAsync(CancellationToken cancellation = default);

        Task AgregarSesionAsync(SesionCaja sesion, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<MovimientoCaja>> ObtenerMovimientosPorSesionAsync(
            int sesionCajaId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<MovimientoCajaDetalleResult>> ObtenerDetalleMovimientosPorSesionAsync(
            int sesionCajaId, CancellationToken cancellationToken = default);

        Task GuardarCambiosAsync(CancellationToken cancellationToken = default);

        Task AgregarMovimientoAsync(MovimientoCaja movimiento, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SesionCaja>> ObtenerHistorialCierresAsync(
            CancellationToken cancellationToken = default);

    }

}
