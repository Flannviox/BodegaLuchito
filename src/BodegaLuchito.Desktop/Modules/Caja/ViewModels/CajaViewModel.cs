using System;
using System.Threading.Tasks;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Application.Caja.UseCases;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Caja.ViewModels
{
    public partial class CajaViewModel : ViewModelBase
    {
        private readonly AbrirCajaUseCase _abrirCajaUseCase;
        private readonly CerrarCajaUseCase _cerrarCajaUseCase;
        private readonly ICajaRepository _cajaRepository;

        //reemplazar cuando autenticacion este listo
     
        private const int UsuarioActualId = 1;

        public CajaViewModel(
            AbrirCajaUseCase abrirCajaUseCase,
            CerrarCajaUseCase cerrarCajaUseCase,
            ICajaRepository cajaRepository)
        {
            _abrirCajaUseCase = abrirCajaUseCase;
            _cerrarCajaUseCase = cerrarCajaUseCase;
            _cajaRepository = cajaRepository;
        }



        [ObservableProperty]
        private EstadoVistaCaja _estadoVista = EstadoVistaCaja.Apertura;

        [ObservableProperty]
        private string? _mensajeError;



        [ObservableProperty]
        private decimal _fondoInicialInput;


        [ObservableProperty]
        private DateTime? _fechaApertura;

        [ObservableProperty]
        private decimal _fondoInicialActual;

        

        [ObservableProperty]
        private decimal _efectivoRealInput;

        [ObservableProperty]
        private decimal _yapeRealInput;

        [ObservableProperty]
        private decimal _plinRealInput;

        [ObservableProperty]
        private string? _observacionInput;

        
        [ObservableProperty]
        private CerrarCajaResult? _resultadoUltimoCierre;

        

        public bool MostrarApertura =>
            EstadoVista == EstadoVistaCaja.Apertura;

        public bool MostrarResumen =>
            EstadoVista == EstadoVistaCaja.EnOperacion;

        public bool MostrarFormularioCierre =>
            EstadoVista == EstadoVistaCaja.Cierre;

        public bool MostrarResultadoCierre =>
            EstadoVista == EstadoVistaCaja.ResultadoCierre;

        public bool CajaAbierta =>
            EstadoVista == EstadoVistaCaja.EnOperacion ||
            EstadoVista == EstadoVistaCaja.Cierre;

        public decimal DiferenciaTotal =>
            ResultadoUltimoCierre is null
                ? 0m
                : ResultadoUltimoCierre.DiferenciaEfectivo
                  + ResultadoUltimoCierre.DiferenciaYape
                  + ResultadoUltimoCierre.DiferenciaPlin;

        
        partial void OnEstadoVistaChanged(EstadoVistaCaja value)
        {
            OnPropertyChanged(nameof(MostrarApertura));
            OnPropertyChanged(nameof(MostrarResumen));
            OnPropertyChanged(nameof(MostrarFormularioCierre));
            OnPropertyChanged(nameof(MostrarResultadoCierre));
            OnPropertyChanged(nameof(CajaAbierta));
        }

        partial void OnResultadoUltimoCierreChanged(CerrarCajaResult? value)
        {
            OnPropertyChanged(nameof(DiferenciaTotal));
        }

        

        public async Task InicializarAsync()
        {
            var sesionAbierta =
                await _cajaRepository.ObtenerSesionAbiertaAsync();

            if (sesionAbierta is null)
            {
                FechaApertura = null;
                FondoInicialActual = 0m;
                EstadoVista = EstadoVistaCaja.Apertura;
                return;
            }

            FechaApertura = sesionAbierta.FechaApertura;
            FondoInicialActual = sesionAbierta.FondoInicial;
            EstadoVista = EstadoVistaCaja.EnOperacion;
        }

        

        [RelayCommand]
        private async Task AbrirCajaAsync()
        {
            MensajeError = null;

            try
            {
                var request = new AbrirCajaRequest(
                    UsuarioActualId,
                    FondoInicialInput);

                var resultado =
                    await _abrirCajaUseCase.EjecutarAsync(request);

                FechaApertura = resultado.FechaApertura;
                FondoInicialActual = resultado.FondoInicial;

                FondoInicialInput = 0m;

                EstadoVista = EstadoVistaCaja.EnOperacion;
            }
            catch (InvalidOperationException ex)
            {
                MensajeError = ex.Message;
            }
        }

        

        [RelayCommand]
        private void AbrirFormularioCierre()
        {
            MensajeError = null;
            EstadoVista = EstadoVistaCaja.Cierre;
        }

        [RelayCommand]
        private void CancelarCierre()
        {
            MensajeError = null;
            EstadoVista = EstadoVistaCaja.EnOperacion;
        }

        

        [RelayCommand]
        private async Task CerrarCajaAsync()
        {
            MensajeError = null;

            try
            {
                var request = new CerrarCajaRequest(
                    UsuarioActualId,
                    EfectivoRealInput,
                    YapeRealInput,
                    PlinRealInput,
                    ObservacionInput);

                ResultadoUltimoCierre =
                    await _cerrarCajaUseCase.EjecutarAsync(request);

                LimpiarFormularioCierre();

                EstadoVista = EstadoVistaCaja.ResultadoCierre;
            }
            catch (InvalidOperationException ex)
            {
                MensajeError = ex.Message;
            }
        }

        

        [RelayCommand]
        private void NuevaApertura()
        {
            MensajeError = null;
            ResultadoUltimoCierre = null;

            FechaApertura = null;
            FondoInicialActual = 0m;

            EstadoVista = EstadoVistaCaja.Apertura;
        }

        

        private void LimpiarFormularioCierre()
        {
            EfectivoRealInput = 0m;
            YapeRealInput = 0m;
            PlinRealInput = 0m;
            ObservacionInput = null;
        }
    }
}
