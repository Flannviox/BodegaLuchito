using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;

namespace BodegaLuchito.Application.Caja.UseCases
{
    public class AbrirCajaUseCase
    {
        private readonly ICajaRepository _cajaRepository;

        public AbrirCajaUseCase(ICajaRepository cajaRepository)
        {
            _cajaRepository = cajaRepository;
        }


        public async Task<AbrirCajaResult> EjecutarAsync(

            AbrirCajaRequest request,
            CancellationToken cancellationToken = default
        ){

            if (request.FondoInicial < 0)
                throw new InvalidOperationException("El fondo inicial no puede ser negativo.");


            var sesionExistente = await _cajaRepository.ObtenerSesionAbiertaAsync(cancellationToken);
            if (sesionExistente is not null)
                throw new InvalidOperationException("Ya existe una sesion de caja abierta");


            var sesion = new SesionCaja
            {
                UsuarioAperturaId = request.UsuarioAperturaId,
                FechaApertura = DateTime.Now,
                FondoInicial = request.FondoInicial,
                Estado = EstadoSesionCaja.Abierta
            };

            await _cajaRepository.AgregarSesionAsync(sesion, cancellationToken);

            return new AbrirCajaResult(sesion.Id, sesion.FechaApertura, sesion.FondoInicial);

        }
    }
}
