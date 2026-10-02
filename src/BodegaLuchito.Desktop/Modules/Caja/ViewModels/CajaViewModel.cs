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
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;


namespace BodegaLuchito.Desktop.Modules.Caja.ViewModels
{
    public sealed class HistorialCierreItem
    {
        public int Id { get; init; }
        public DateTime FechaApertura { get; init; }
        public DateTime? FechaCierre { get; init; }
        public decimal FondoInicial { get; init; }
        public decimal? EfectivoEsperado { get; init; }
        public decimal? EfectivoReal { get; init; }
        public decimal? DiferenciaEfectivo { get; init; }
        public decimal? YapeNeto { get; init; }
        public decimal? PlinNeto { get; init; }
        public string? ObservacionCierre { get; init; }
    }
    public partial class CajaViewModel : ViewModelBase
    {
        private readonly AbrirCajaUseCase _abrirCajaUseCase;
        private readonly CerrarCajaUseCase _cerrarCajaUseCase;
        private readonly ConsultarHistorialCierresUseCase _consultarHistorialCierresUseCase;
        private readonly ICajaRepository _cajaRepository;
        private readonly ISesionUsuario _sesionUsuario;
        private decimal? _montoMinimoAplicado;
        private decimal? _montoMaximoAplicado;
        public CajaViewModel(
            AbrirCajaUseCase abrirCajaUseCase,
            CerrarCajaUseCase cerrarCajaUseCase,
            ConsultarHistorialCierresUseCase consultarHistorialCierresUseCase,
            ICajaRepository cajaRepository,
            ISesionUsuario sesionUsuario)
        {
            _abrirCajaUseCase = abrirCajaUseCase;
            _cerrarCajaUseCase = cerrarCajaUseCase;
            _consultarHistorialCierresUseCase = consultarHistorialCierresUseCase;
            _cajaRepository = cajaRepository;
            _sesionUsuario = sesionUsuario;
            HistorialCierresVista = CollectionViewSource.GetDefaultView(HistorialCierres);

            HistorialCierresVista.Filter = FiltrarCierre;
        }

        [ObservableProperty]
        private EstadoVistaCaja _estadoVista = EstadoVistaCaja.Apertura;
        private EstadoVistaCaja _estadoAnteriorHistorial = EstadoVistaCaja.Apertura;

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

        [ObservableProperty]
        private DateTime? _fechaDesdeFiltro;

        [ObservableProperty]
        private DateTime? _fechaHastaFiltro;

        [ObservableProperty]
        private string _montoMinimoFiltroInput = string.Empty;

        [ObservableProperty]
        private string _montoMaximoFiltroInput = string.Empty;

        [ObservableProperty]
        private int _cantidadCierresMostrados;
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

        public bool MostrarHistorial =>
            EstadoVista == EstadoVistaCaja.Historial;

        public ObservableCollection<HistorialCierreItem> HistorialCierres { get; } = new();
        public ICollectionView HistorialCierresVista { get; }

        partial void OnEstadoVistaChanged(EstadoVistaCaja value)
        {
            OnPropertyChanged(nameof(MostrarApertura));
            OnPropertyChanged(nameof(MostrarResumen));
            OnPropertyChanged(nameof(MostrarFormularioCierre));
            OnPropertyChanged(nameof(MostrarResultadoCierre));
            OnPropertyChanged(nameof(CajaAbierta));
            OnPropertyChanged(nameof(MostrarHistorial));
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
                MensajeError = "Ingresa un monto válido para el fondo inicial.";
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

        [RelayCommand]
        private async Task AbrirHistorialAsync()
        {
            MensajeError = null;

            var cierres =
                await _consultarHistorialCierresUseCase.EjecutarAsync();

            HistorialCierres.Clear();

            foreach (var cierre in cierres)
            {
                HistorialCierres.Add(new HistorialCierreItem
                {
                    Id = cierre.Id,
                    FechaApertura = cierre.FechaApertura,
                    FechaCierre = cierre.FechaCierre,
                    FondoInicial = cierre.FondoInicial,

                    EfectivoEsperado = cierre.EfectivoEsperado,
                    EfectivoReal = cierre.EfectivoReal,
                    DiferenciaEfectivo = cierre.DiferenciaEfectivo,

                    YapeNeto = cierre.YapeEsperado,
                    PlinNeto = cierre.PlinEsperado,

                    ObservacionCierre = cierre.ObservacionCierre
                });
            }
            HistorialCierresVista.Refresh();
            ActualizarCantidadCierresMostrados();

            _estadoAnteriorHistorial = EstadoVista;
            EstadoVista = EstadoVistaCaja.Historial;
        }
        private void LimpiarFormularioCierre()
        {
            EfectivoRealInput = "0";
            ObservacionInput = null;
        }

        [RelayCommand]
        private void VolverDesdeHistorial()
        {
            MensajeError = null;
            EstadoVista = _estadoAnteriorHistorial;
        }

        [RelayCommand]
        private void AplicarFiltrosHistorial()
        {
            MensajeError = null;

            if (FechaDesdeFiltro.HasValue &&
                FechaHastaFiltro.HasValue &&
                FechaDesdeFiltro.Value.Date > FechaHastaFiltro.Value.Date)
            {
                MensajeError =
                    "La fecha desde no puede ser posterior a la fecha hasta.";
                return;
            }

            decimal? montoMinimo = null;
            decimal? montoMaximo = null;

            if (!string.IsNullOrWhiteSpace(MontoMinimoFiltroInput))
            {
                if (!TryParseMonto(MontoMinimoFiltroInput, out var minimo))
                {
                    MensajeError = "Ingresa un monto mínimo válido.";
                    return;
                }

                montoMinimo = minimo;
            }

            if (!string.IsNullOrWhiteSpace(MontoMaximoFiltroInput))
            {
                if (!TryParseMonto(MontoMaximoFiltroInput, out var maximo))
                {
                    MensajeError = "Ingresa un monto máximo válido.";
                    return;
                }

                montoMaximo = maximo;
            }

            if (montoMinimo.HasValue &&
                montoMaximo.HasValue &&
                montoMinimo.Value > montoMaximo.Value)
            {
                MensajeError = "El monto mínimo no puede ser mayor al monto máximo.";
                return;
            }
            _montoMinimoAplicado = montoMinimo;
            _montoMaximoAplicado = montoMaximo;
            HistorialCierresVista.Refresh();
            ActualizarCantidadCierresMostrados();
        }

        [RelayCommand]
        private void LimpiarFiltrosHistorial()
        {
            MensajeError = null;
            FechaDesdeFiltro = null;
            FechaHastaFiltro = null;
            MontoMinimoFiltroInput = string.Empty;
            MontoMaximoFiltroInput = string.Empty;
            _montoMinimoAplicado = null;
            _montoMaximoAplicado = null;
            HistorialCierresVista.Refresh();
            ActualizarCantidadCierresMostrados();
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
        private bool FiltrarCierre(object item)
        {
            if (item is not HistorialCierreItem cierre)
                return false;

            if (FechaDesdeFiltro.HasValue &&
                cierre.FechaCierre.HasValue &&
                cierre.FechaCierre.Value.Date < FechaDesdeFiltro.Value.Date)
            {
                return false;
            }

            if (FechaHastaFiltro.HasValue &&
                cierre.FechaCierre.HasValue &&
                cierre.FechaCierre.Value.Date > FechaHastaFiltro.Value.Date)
            {
                return false;
            }

            if (_montoMinimoAplicado.HasValue &&
                (!cierre.EfectivoReal.HasValue ||
                 cierre.EfectivoReal.Value < _montoMinimoAplicado.Value))
            {
                return false;
            }

            if (_montoMaximoAplicado.HasValue &&
                (!cierre.EfectivoReal.HasValue ||
                 cierre.EfectivoReal.Value > _montoMaximoAplicado.Value))
            {
                return false;
            }
            return true;
        }
        private void ActualizarCantidadCierresMostrados()
        {
            CantidadCierresMostrados =
                HistorialCierresVista.Cast<object>().Count();
        }
    }
}
