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

        ){

            if(request.EfectivoReal < 0 || request.YapeReal < 0 || request.PlinReal < 0)
            {
                throw new ArgumentException("Los montos reales no pueden ser negativos.");
            }


            var sesion = await _cajaRepository.ObtenerSesionAbiertaAsync(cancellationToken)
            ?? throw new InvalidOperationException("No existe una sesión de caja abierta.");

            var movimientos = await _cajaRepository.ObtenerMovimientosPorSesionAsync(

                sesion.Id,
                cancellationToken
            );

            decimal CalcularEsperado(

                IEnumerable<MovimientoCaja>movimientoCajas,
                MetodoPago metodoPago,
                decimal fondoInicial =0

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

                return fondoInicial + ingresos - egresos - reversiones;


            }

            var efectivoEsperado = CalcularEsperado(
                movimientos,
                MetodoPago.Efectivo,
                sesion.FondoInicial);

            var yapeEsperado = CalcularEsperado(
                movimientos,
                MetodoPago.Yape);

            var plinEsperado = CalcularEsperado(
                movimientos,
                MetodoPago.Plin);

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
