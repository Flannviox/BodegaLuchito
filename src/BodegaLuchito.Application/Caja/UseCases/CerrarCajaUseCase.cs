using System;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Application.Caja.UseCases
{
    public class CerrarCajaUseCase
    {

        private readonly ICajaRepository _cajaRepository;

        public CerrarCajaUseCase(ICajaRepository cajaRepository)
        {
            _cajaRepository = cajaRepository;
        }



        public async Task<CerrarCajaResult> EjecutarAsync(

            CerrarCajaRequest request,
            CancellationToken cancellationToken = default

        )
        {

            if (request.EfectivoReal < 0)
            {
                throw new ArgumentException("El efectivo contado no puede ser negativo");
            }


            var sesion = await _cajaRepository.ObtenerSesionAbiertaAsync(cancellationToken)
            ?? throw new InvalidOperationException("No existe una sesión de caja abierta.");

            var movimientos = await _cajaRepository.ObtenerMovimientosPorSesionAsync(

                sesion.Id,
                cancellationToken
            );

            (decimal ingresos, decimal salidas, decimal neto)

             CalcularMovimientos(IEnumerable<MovimientoCaja> movimientoCajas, MetodoPago metodoPago)
            {
                var ingresos = movimientoCajas
                    .Where(m =>
                        m.MetodoPago == metodoPago &&
                        m.Tipo == TipoMovimientoCaja.IngresoVenta)
                    .Sum(m => m.Monto);

                var egresos = movimientoCajas
                    .Where(m =>
                        m.MetodoPago == metodoPago &&
                        m.Tipo == TipoMovimientoCaja.EgresoAbastecimiento)
                    .Sum(m => m.Monto);

                var reversiones = movimientoCajas
                    .Where(m =>
                        m.MetodoPago == metodoPago &&
                        m.Tipo == TipoMovimientoCaja.ReversionVenta)
                    .Sum(m => m.Monto);

                var salidas = egresos + reversiones;
                var neto = ingresos - salidas;

                return (ingresos, salidas, neto);
            }

            var efectivo =
                CalcularMovimientos(
                 movimientos,
                 MetodoPago.Efectivo);

            var yape =
                CalcularMovimientos(
                    movimientos,
                    MetodoPago.Yape);

            var plin =
                CalcularMovimientos(
                    movimientos,
                    MetodoPago.Plin);

            var efectivoEsperado = sesion.FondoInicial + efectivo.neto;

            sesion.EfectivoEsperado = efectivoEsperado;
            sesion.EfectivoReal = request.EfectivoReal;
            sesion.DiferenciaEfectivo = request.EfectivoReal - efectivoEsperado;

            sesion.YapeEsperado = yape.neto;
            sesion.YapeReal = null;
            sesion.DiferenciaYape = null;

            sesion.PlinEsperado = plin.neto;
            sesion.PlinReal = null;
            sesion.DiferenciaPlin = null;

            sesion.ObservacionCierre = request.ObservacionCierre;
            sesion.UsuarioCierreId = request.UsuarioCierreId;
            sesion.FechaCierre = DateTime.Now;
            sesion.Estado = EstadoSesionCaja.Cerrada;

            await _cajaRepository.GuardarCambiosAsync(cancellationToken);

            return new CerrarCajaResult(
                efectivoEsperado,
                request.EfectivoReal,
                sesion.DiferenciaEfectivo.Value,

                yape.ingresos,
                yape.salidas,
                yape.neto,

                plin.ingresos,
                plin.salidas,
                plin.neto
            );
        }
    }
}
