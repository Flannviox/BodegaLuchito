using System;
using System.Globalization;
using System.Threading.Tasks;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Application.Caja.UseCases;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BodegaLuchito.Application.Common.Session;

namespace BodegaLuchito.Desktop.Modules.Caja.ViewModels
{
    public partial class CajaViewModel : ViewModelBase
    {
        private readonly AbrirCajaUseCase _abrirCajaUseCase;
        private readonly CerrarCajaUseCase _cerrarCajaUseCase;
        private readonly ICajaRepository _cajaRepository;
        private readonly ISesionUsuario _sesionUsuario;


        public CajaViewModel(
            AbrirCajaUseCase abrirCajaUseCase,
            CerrarCajaUseCase cerrarCajaUseCase,
            ICajaRepository cajaRepository,
            ISesionUsuario sesionUsuario)
        {
            _abrirCajaUseCase = abrirCajaUseCase;
            _cerrarCajaUseCase = cerrarCajaUseCase;
            _cajaRepository = cajaRepository;
            _sesionUsuario = sesionUsuario;
        }


        [ObservableProperty]
        private EstadoVistaCaja _estadoVista = EstadoVistaCaja.Apertura;

        [ObservableProperty]
        private string? _mensajeError;

        [ObservableProperty]
        private string _fondoInicialInput = "0";

        [ObservableProperty]
        private DateTime? _fechaApertura;

        [ObservableProperty]
        private decimal _fondoInicialActual;

        [ObservableProperty]
        private string _efectivoRealInput = "0";

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
        partial void OnEstadoVistaChanged(EstadoVistaCaja value)
        {
            OnPropertyChanged(nameof(MostrarApertura));
            OnPropertyChanged(nameof(MostrarResumen));
            OnPropertyChanged(nameof(MostrarFormularioCierre));
            OnPropertyChanged(nameof(MostrarResultadoCierre));
            OnPropertyChanged(nameof(CajaAbierta));
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
            var usuarioActual = _sesionUsuario.UsuarioActual;

            if (usuarioActual is null)
            {
                MensajeError = "No existe un usuario autenticado.";
                return;
            }
            if (!TryParseMonto(FondoInicialInput, out var fondoInicial))
            {
                MensajeError =
                    "Ingresa un monto válido para el fondo inicial.";

                return;
            }
            try
            {
                var request = new AbrirCajaRequest(
                    usuarioActual.IdUsuario,
                    fondoInicial);

                var resultado =
                    await _abrirCajaUseCase.EjecutarAsync(request);

                FechaApertura = resultado.FechaApertura;
                FondoInicialActual = resultado.FondoInicial;

                FondoInicialInput = "0";

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

            var usuarioActual = _sesionUsuario.UsuarioActual;

            if (usuarioActual is null)
            {
                MensajeError = "No existe un usuario autenticado.";
                return;
            }

            if (!TryParseMonto(EfectivoRealInput, out var efectivoReal))
            {
                MensajeError =
                    "Ingresa un monto válido para el efectivo.";

                return;
            }

            try
            {
                var request = new CerrarCajaRequest(
                    usuarioActual.IdUsuario,
                    efectivoReal,
                    ObservacionInput);

                ResultadoUltimoCierre =
                    await _cerrarCajaUseCase.EjecutarAsync(request);

                LimpiarFormularioCierre();

                EstadoVista = EstadoVistaCaja.ResultadoCierre;
            }
            catch (ArgumentException ex)
            {
                MensajeError = ex.Message;
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
            EfectivoRealInput = "0";
            ObservacionInput = null;
        }

        private static bool TryParseMonto(string texto, out decimal monto)
        {
            var textoNormalizado = texto
                .Trim()
                .Replace(',', '.');

            return decimal.TryParse(
                textoNormalizado,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out monto);
        }
    }
}
