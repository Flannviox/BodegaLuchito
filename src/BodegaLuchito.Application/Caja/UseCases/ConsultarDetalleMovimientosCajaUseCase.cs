using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.Interfaces;

namespace BodegaLuchito.Application.Caja.UseCases
{
    public sealed class ConsultarDetalleMovimientosCajaUseCase
    {
        private readonly ICajaRepository _cajaRepository;

        public ConsultarDetalleMovimientosCajaUseCase(
        ICajaRepository cajaRepository)
        {
            _cajaRepository = cajaRepository;
        }

       public Task<IReadOnlyList<MovimientoCajaDetalleResult>>EjecutarAsync(
           int sesionCajaId, CancellationToken cancellationToken = default)
        {
            if (sesionCajaId <= 0)
            {
                throw new ArgumentException("La sesión de caja no es válida.");
            }

            return _cajaRepository
                .ObtenerDetalleMovimientosPorSesionAsync(sesionCajaId,cancellationToken);
        }
    }
}
