using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Shared.Enums;
using static System.Collections.Specialized.BitVector32;

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

        ){


            var sesion = await _cajaRepository.ObtenerSesionAbiertaAsync(cancellationToken)
            ?? throw new InvalidOperationException("No existe una sesión de caja abierta.");

            var movimientos = await _cajaRepository.ObtenerMovimientosPorSesionAsync(

                sesion.Id,
                cancellationToken
            );

            decimal CalcularEsperado(

                MetodoPago metodoPago,
                bool incluirFondoInicial

            ){

                var ingresos = movimientos
                    .Where(m => m.MetodoPago == metodoPago && m.Tipo == TipoMovimientoCaja.IngresoVenta)
                    .Sum(m => m.Monto);

                var egresos = movimientos
                .Where(m => m.MetodoPago == metodoPago && m.Tipo == TipoMovimientoCaja.EgresoAbastecimiento)
                .Sum(m => m.Monto);

                var reversiones = movimientos
                    .Where(m => m.MetodoPago == metodoPago && m.Tipo == TipoMovimientoCaja.ReversionVenta)
                    .Sum(m => m.Monto);

                var esperado = ingresos - egresos - reversiones;
                return incluirFondoInicial ? esperado + sesion.FondoInicial : esperado;

            }

            var efectivoEsperado = CalcularEsperado(MetodoPago.Efectivo, incluirFondoInicial: true);
            var yapeEsperado = CalcularEsperado(MetodoPago.Yape, incluirFondoInicial: false);
            var plinEsperado = CalcularEsperado(MetodoPago.Plin, incluirFondoInicial: false);

            sesion.EfectivoEsperado = efectivoEsperado;
            sesion.EfectivoReal = request.EfectivoReal;
            sesion.DiferenciaEfectivo = request.EfectivoReal - efectivoEsperado;

            sesion.YapeEsperado = yapeEsperado;
            sesion.YapeReal = request.YapeReal;
            sesion.DiferenciaYape = request.YapeReal - yapeEsperado;

            sesion.PlinEsperado = plinEsperado;
            sesion.PlinReal = request.PlinReal;
            sesion.DiferenciaPlin = request.PlinReal - plinEsperado;

            sesion.ObservacionCierre = request.ObservacionCierre;
            sesion.UsuarioCierreId = request.UsuarioCierreId;
            sesion.FechaCierre = DateTime.Now;
            sesion.Estado = EstadoSesionCaja.Cerrada;

            await _cajaRepository.GuardarCambiosAsync(cancellationToken);

            return new CerrarCajaResult(
                efectivoEsperado, request.EfectivoReal, sesion.DiferenciaEfectivo.Value,
                yapeEsperado, request.YapeReal, sesion.DiferenciaYape.Value,
                plinEsperado, request.PlinReal, sesion.DiferenciaPlin.Value);

        }


    }
}
