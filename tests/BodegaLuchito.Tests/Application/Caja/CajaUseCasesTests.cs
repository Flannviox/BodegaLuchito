using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.UseCases;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Infrastructure.Caja.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Tests.Application.Caja
{
    public sealed class CajaUseCasesTests
    {

        private static async Task<(SqliteConnection connection, DbContextOptions<BodegaLuchitoDbContext> options)>
        CrearBaseEnMemoriaAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<BodegaLuchitoDbContext>()
                .UseSqlite(connection)
                .Options;

            await using var context = new BodegaLuchitoDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return (connection, options);
        }

        [Fact]
        public async Task AbrirCaja_ConDatosValidos_DebeCrearSesionAbierta()
        {
            var (connection, options) = await CrearBaseEnMemoriaAsync();
            await using var connectionDispose = connection;

            await using var context = new BodegaLuchitoDbContext(options);
            var repositorio = new CajaRepository(context);
            var useCase = new AbrirCajaUseCase(repositorio);

            var request = new AbrirCajaRequest(UsuarioAperturaId: 1, FondoInicial: 100m);

            var resultado = await useCase.EjecutarAsync(request);

            Assert.True(resultado.SesionCajaId > 0);
            Assert.Equal(100m, resultado.FondoInicial);

            var sesionGuardada = await context.Set<SesionCaja>().SingleAsync();
            Assert.Equal(EstadoSesionCaja.Abierta, sesionGuardada.Estado);
        }



        [Fact]
        public async Task AbrirCaja_ConFondoInicialNegativo_DebeSerRechazado()
        {
            var (connection, options) = await CrearBaseEnMemoriaAsync();
            await using var connectionDispose = connection;

            await using var context = new BodegaLuchitoDbContext(options);
            var useCase = new AbrirCajaUseCase(new CajaRepository(context));

            var request = new AbrirCajaRequest(UsuarioAperturaId: 1, FondoInicial: -50m);

            var action = async () => await useCase.EjecutarAsync(request);

            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        [Fact]
        public async Task AbrirCaja_ConSesionYaAbierta_DebeSerRechazado()
        {
            var (connection, options) = await CrearBaseEnMemoriaAsync();
            await using var connectionDispose = connection;

            await using var context = new BodegaLuchitoDbContext(options);
            var repositorio = new CajaRepository(context);

            var sesionExistente = new SesionCaja
            {
                UsuarioAperturaId = 1,
                FechaApertura = DateTime.Now,
                FondoInicial = 100m,
                Estado = EstadoSesionCaja.Abierta
            };

            context.Set<SesionCaja>().Add(sesionExistente);
            await context.SaveChangesAsync();

            var useCase = new AbrirCajaUseCase(repositorio);

            var request = new AbrirCajaRequest(
                UsuarioAperturaId: 2,
                FondoInicial: 50m);

            var action = async () => await useCase.EjecutarAsync(request);

            var exception =
                await Assert.ThrowsAsync<InvalidOperationException>(action);

            Assert.Equal(
                "Ya existe una sesion de caja abierta",
                exception.Message);
        }




        [Fact]
        public async Task CerrarCaja_SinSesionAbierta_DebeSerRechazado()
        {
           
            var (connection, options) = await CrearBaseEnMemoriaAsync();
            await using var connectionDispose = connection;

            await using var context = new BodegaLuchitoDbContext(options);
            var useCase = new CerrarCajaUseCase(new CajaRepository(context));

            var request = new CerrarCajaRequest(
                UsuarioCierreId: 1, EfectivoReal: 0m, YapeReal: 0m, PlinReal: 0m, ObservacionCierre: null);

            
            var action = async () => await useCase.EjecutarAsync(request);

            
            await Assert.ThrowsAsync<InvalidOperationException>(action);
        }

        [Fact]
        public async Task CerrarCaja_ConSesionAbierta_DebeCerrarlaCorrectamente()
        {
            
            var (connection, options) = await CrearBaseEnMemoriaAsync();
            await using var connectionDispose = connection;

            await using var context = new BodegaLuchitoDbContext(options);
            var repositorio = new CajaRepository(context);

            var sesion = new SesionCaja
            {
                UsuarioAperturaId = 1,
                FechaApertura = DateTime.Now,
                FondoInicial = 100m,
                Estado = EstadoSesionCaja.Abierta
            };
            context.Set<SesionCaja>().Add(sesion);
            await context.SaveChangesAsync();

            var useCase = new CerrarCajaUseCase(repositorio);
            var request = new CerrarCajaRequest(
                UsuarioCierreId: 2, EfectivoReal: 100m, YapeReal: 0m, PlinReal: 0m, ObservacionCierre: "Cierre normal");

           
            var resultado = await useCase.EjecutarAsync(request);

            
            Assert.Equal(EstadoSesionCaja.Cerrada, (await context.Set<SesionCaja>().SingleAsync()).Estado);
            Assert.Equal(0m, resultado.DiferenciaEfectivo);
        }

        [Fact]
        public async Task CerrarCaja_DebeCalcularDiferenciasCorrectamente()
        {
           
            var (connection, options) = await CrearBaseEnMemoriaAsync();
            await using var connectionDispose = connection;

            await using var context = new BodegaLuchitoDbContext(options);
            var repositorio = new CajaRepository(context);

            var sesion = new SesionCaja
            {
                UsuarioAperturaId = 1,
                FechaApertura = DateTime.Now,
                FondoInicial = 100m,
                Estado = EstadoSesionCaja.Abierta
            };
            context.Set<SesionCaja>().Add(sesion);
            await context.SaveChangesAsync();

            context.Set<MovimientoCaja>().AddRange(
                new MovimientoCaja
                {
                    SesionCajaId = sesion.Id,
                    UsuarioId = 1,
                    Tipo = TipoMovimientoCaja.IngresoVenta,
                    MetodoPago = MetodoPago.Efectivo,
                    Monto = 50m,
                    FechaHora = DateTime.Now
                },
                new MovimientoCaja
                {
                    SesionCajaId = sesion.Id,
                    UsuarioId = 1,
                    Tipo = TipoMovimientoCaja.EgresoAbastecimiento,
                    MetodoPago = MetodoPago.Efectivo,
                    Monto = 20m,
                    FechaHora = DateTime.Now
                });
            await context.SaveChangesAsync();

            
            var useCase = new CerrarCajaUseCase(repositorio);
            var request = new CerrarCajaRequest(
                UsuarioCierreId: 2, EfectivoReal: 125m, YapeReal: 0m, PlinReal: 0m, ObservacionCierre: null);

            
            var resultado = await useCase.EjecutarAsync(request);

            
            Assert.Equal(130m, resultado.EfectivoEsperado);
            Assert.Equal(125m, resultado.EfectivoReal);
            Assert.Equal(-5m, resultado.DiferenciaEfectivo); 
        }

        [Fact]
        public async Task CerrarCaja_ConMontoNegativo_DebeSerRechazado()
        {
            var (connection, options) = await CrearBaseEnMemoriaAsync();
            await using var connectionDispose = connection;

            await using var context = new BodegaLuchitoDbContext(options);
            var repositorio = new CajaRepository(context);

            var sesion = new SesionCaja
            {
                UsuarioAperturaId = 1,
                FechaApertura = DateTime.Now,
                FondoInicial = 100m,
                Estado = EstadoSesionCaja.Abierta
            };

            context.Set<SesionCaja>().Add(sesion);
            await context.SaveChangesAsync();

            var useCase = new CerrarCajaUseCase(repositorio);

            var request = new CerrarCajaRequest(
                UsuarioCierreId: 1,
                EfectivoReal: -10m,
                YapeReal: 0m,
                PlinReal: 0m,
                ObservacionCierre: null);

            var action = async () => await useCase.EjecutarAsync(request);

            await Assert.ThrowsAsync<ArgumentException>(action);
        }

    }
}
