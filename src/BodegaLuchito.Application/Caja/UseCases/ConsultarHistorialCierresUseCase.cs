using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Domain.Caja.Entities;

namespace BodegaLuchito.Application.Caja.UseCases
{
    public class ConsultarHistorialCierresUseCase
    {
        private readonly ICajaRepository _cajaRepository;

        public ConsultarHistorialCierresUseCase(
            ICajaRepository cajaRepository)
        {
            _cajaRepository = cajaRepository;
        }

        public Task <IReadOnlyList<SesionCaja>>EjecutarAsync(
            CancellationToken cancellationToken = default)
        {
            return _cajaRepository.ObtenerHistorialCierresAsync(cancellationToken);
        }
    }
}
