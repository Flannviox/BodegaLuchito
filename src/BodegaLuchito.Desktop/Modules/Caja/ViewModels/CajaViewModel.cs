using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using BodegaLuchito.Application.Autenticacion.UseCases;
using BodegaLuchito.Application.Caja.DTOs;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Application.Caja.UseCases;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Desktop.Common.ViewModels;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Shared.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Caja.ViewModels
{
    public sealed class UsuarioFiltroCajaItem
    {
        public int? Id { get; init; }
        public string Nombre { get; init; } = string.Empty;
    }
    public sealed class HistorialCierreItem
    {
        public int Id { get; init; }
        public int UsuarioAperturaId { get; init; }
        public string UsuarioAperturaNombre { get; init; } = string.Empty;
        public DateTime FechaApertura { get; init; }
        public DateTime? FechaCierre { get; init; }
        public decimal FondoInicial { get; init; }
        public decimal? EfectivoEsperado { get; init; }
        public decimal? EfectivoReal { get; init; }
        public decimal? DiferenciaEfectivo { get; init; }
        public decimal? YapeNeto { get; init; }
        public decimal? PlinNeto { get; init; }
        public string? ObservacionCierre { get; init; }
        public bool DescuadreResuelto { get; init; }
        public DateTime? FechaResolucionDescuadre { get; init; }
        public int? UsuarioResolucionId { get; init; }
        public string? ObservacionResolucionDescuadre { get; init; }

        public string EstadoDescuadre =>
            !DiferenciaEfectivo.HasValue || DiferenciaEfectivo.Value == 0
                ? "Sin descuadre"
                : DescuadreResuelto ? "Resuelto" : "Pendiente";
    }
    public partial class CajaViewModel : ViewModelBase
    {
        private readonly AbrirCajaUseCase _abrirCajaUseCase;
        private readonly CerrarCajaUseCase _cerrarCajaUseCase;
        private readonly ConsultarHistorialCierresUseCase _consultarHistorialCierresUseCase;
        private readonly ConsultarDetalleMovimientosCajaUseCase _consultarDetalleMovimientosCajaUseCase;
        private readonly ResolverDescuadreCajaUseCase _resolverDescuadreCajaUseCase;
        private readonly ListarUsuariosUseCase _listarUsuariosUseCase;
        private readonly ICajaRepository _cajaRepository;
        private readonly ISesionUsuario _sesionUsuario;
        private decimal? _montoMinimoAplicado;
        private decimal? _montoMaximoAplicado;
        private int? _sesionCajaActualId;
        private EstadoVistaCaja _estadoAnteriorHistorial = EstadoVistaCaja.Apertura;

        public CajaViewModel(
            AbrirCajaUseCase abrirCajaUseCase,
            CerrarCajaUseCase cerrarCajaUseCase,
            ConsultarDetalleMovimientosCajaUseCase consultarDetalleMovimientosCajaUseCase,
            ConsultarHistorialCierresUseCase consultarHistorialCierresUseCase,
            ResolverDescuadreCajaUseCase resolverDescuadreCajaUseCase,
            ListarUsuariosUseCase listarUsuariosUseCase,
            ICajaRepository cajaRepository,
            ISesionUsuario sesionUsuario)
        {
            _abrirCajaUseCase = abrirCajaUseCase;
            _cerrarCajaUseCase = cerrarCajaUseCase;
            _consultarDetalleMovimientosCajaUseCase = consultarDetalleMovimientosCajaUseCase;
            _consultarHistorialCierresUseCase = consultarHistorialCierresUseCase;
            _resolverDescuadreCajaUseCase = resolverDescuadreCajaUseCase;
            _listarUsuariosUseCase = listarUsuariosUseCase;
            _cajaRepository = cajaRepository;
            _sesionUsuario = sesionUsuario;

            HistorialCierresVista = CollectionViewSource.GetDefaultView(HistorialCierres);
            HistorialCierresVista.Filter = FiltrarCierre;
            MovimientosCajaActualVista = CollectionViewSource.GetDefaultView(MovimientosCajaActual);
            MovimientosCajaActualVista.Filter = FiltrarMovimientoCajaActual;
        }

        [ObservableProperty] private EstadoVistaCaja _estadoVista = EstadoVistaCaja.Apertura;
        [ObservableProperty] private string? _mensajeError;
        [ObservableProperty] private string _fondoInicialInput = "0";
        [ObservableProperty] private DateTime? _fechaApertura;
        [ObservableProperty] private decimal _fondoInicialActual;
        [ObservableProperty] private string _efectivoRealInput = "0";
        [ObservableProperty] private string? _observacionInput;
        [ObservableProperty] private CerrarCajaResult? _resultadoUltimoCierre;
        [ObservableProperty] private DateTime? _fechaDesdeFiltro;
        [ObservableProperty] private DateTime? _fechaHastaFiltro;
        [ObservableProperty] private string _montoMinimoFiltroInput = string.Empty;
        [ObservableProperty] private string _montoMaximoFiltroInput = string.Empty;
        [ObservableProperty] private int _cantidadCierresMostrados;
        [ObservableProperty] private HistorialCierreItem? _cierreSeleccionado;
        [ObservableProperty] private bool _mostrarDetalleCierre;
        [ObservableProperty] private UsuarioFiltroCajaItem? _usuarioFiltroSeleccionado;
        [ObservableProperty] private string _estadoDescuadreFiltroSeleccionado = "Todos";
        [ObservableProperty] private bool _mostrarResolucionDescuadre;
        [ObservableProperty] private string? _observacionResolucionDescuadreInput;
        [ObservableProperty] private string _tipoMovimientoActualFiltroSeleccionado = "Todos";
        [ObservableProperty] private string _metodoPagoActualFiltroSeleccionado = "Todos";
        [ObservableProperty] private decimal _ingresosEfectivoActual;
        [ObservableProperty] private decimal _egresosEfectivoActual;
        [ObservableProperty] private decimal _reversionesEfectivoActual;
        [ObservableProperty] private decimal _efectivoEsperadoActual;
        [ObservableProperty] private decimal _yapeNetoActual;
        [ObservableProperty] private decimal _plinNetoActual;

        public bool MostrarApertura => EstadoVista == EstadoVistaCaja.Apertura;
        public bool MostrarResumen => EstadoVista == EstadoVistaCaja.EnOperacion;
        public bool MostrarFormularioCierre => EstadoVista == EstadoVistaCaja.Cierre;
        public bool MostrarResultadoCierre => EstadoVista == EstadoVistaCaja.ResultadoCierre;
        public bool CajaAbierta => EstadoVista is EstadoVistaCaja.EnOperacion or EstadoVistaCaja.Cierre;
        public bool MostrarHistorial => EstadoVista == EstadoVistaCaja.Historial;

        public bool PuedeResolverDescuadre =>
            _sesionUsuario.UsuarioActual?.EsAdministradora == true &&
            CierreSeleccionado is not null &&
            CierreSeleccionado.DiferenciaEfectivo.HasValue &&
            CierreSeleccionado.DiferenciaEfectivo.Value != 0 &&
            !CierreSeleccionado.DescuadreResuelto;

        public ObservableCollection<HistorialCierreItem> HistorialCierres { get; } = new();
        public ObservableCollection<MovimientoCajaDetalleResult> MovimientosCierreSeleccionado { get; } = new();
        public ObservableCollection<MovimientoCajaDetalleResult> MovimientosCajaActual { get; } = new();
        public ObservableCollection<UsuarioFiltroCajaItem> UsuariosFiltro { get; } = new();

        public ObservableCollection<string> EstadosDescuadreFiltro { get; } = new()
        {
            "Todos", "Sin descuadre", "Pendiente", "Resuelto"
        };

        public ObservableCollection<string> TiposMovimientoActualFiltro { get; } = new()
        {
            "Todos", "Ingresos", "Egresos", "Reversiones"
        };

        public ObservableCollection<string> MetodosPagoActualFiltro { get; } = new()
        {
            "Todos", "Efectivo", "Yape", "Plin"
        };

        public ICollectionView HistorialCierresVista { get; }
        public ICollectionView MovimientosCajaActualVista { get; }
        partial void OnEstadoVistaChanged(EstadoVistaCaja value)
        {
            OnPropertyChanged(nameof(MostrarApertura));
            OnPropertyChanged(nameof(MostrarResumen));
            OnPropertyChanged(nameof(MostrarFormularioCierre));
            OnPropertyChanged(nameof(MostrarResultadoCierre));
            OnPropertyChanged(nameof(CajaAbierta));
            OnPropertyChanged(nameof(MostrarHistorial));
        }

        partial void OnCierreSeleccionadoChanged(HistorialCierreItem? value) =>
            OnPropertyChanged(nameof(PuedeResolverDescuadre));

        partial void OnTipoMovimientoActualFiltroSeleccionadoChanged(string value) =>
            MovimientosCajaActualVista.Refresh();

        partial void OnMetodoPagoActualFiltroSeleccionadoChanged(string value) =>
            MovimientosCajaActualVista.Refresh();

        public async Task InicializarAsync()
        {
            var sesionAbierta = await _cajaRepository.ObtenerSesionAbiertaAsync();

            if (sesionAbierta is null)
            {
                _sesionCajaActualId = null;
                FechaApertura = null;
                FondoInicialActual = 0m;
                LimpiarMovimientosCajaActual();
                EstadoVista = EstadoVistaCaja.Apertura;
                return;
            }

            _sesionCajaActualId = sesionAbierta.Id;
            FechaApertura = sesionAbierta.FechaApertura;
            FondoInicialActual = sesionAbierta.FondoInicial;
            await CargarMovimientosCajaActualAsync();
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
                var request = new AbrirCajaRequest(usuarioActual.IdUsuario, fondoInicial);
                var resultado = await _abrirCajaUseCase.EjecutarAsync(request);
                _sesionCajaActualId = resultado.SesionCajaId;
                FechaApertura = resultado.FechaApertura;
                FondoInicialActual = resultado.FondoInicial;
                FondoInicialInput = "0";
                await CargarMovimientosCajaActualAsync();
                EstadoVista = EstadoVistaCaja.EnOperacion;
            }
            catch (InvalidOperationException ex) { MensajeError = ex.Message; }
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
                MensajeError = "Ingresa un monto válido para el efectivo.";
                return;
            }

            try
            {
                var request = new CerrarCajaRequest(usuarioActual.IdUsuario, efectivoReal, ObservacionInput);
                ResultadoUltimoCierre = await _cerrarCajaUseCase.EjecutarAsync(request);
                _sesionCajaActualId = null;
                LimpiarMovimientosCajaActual();
                LimpiarFormularioCierre();
                EstadoVista = EstadoVistaCaja.ResultadoCierre;
            }
            catch (ArgumentException ex) { MensajeError = ex.Message; }
            catch (InvalidOperationException ex) { MensajeError = ex.Message; }
        }

        [RelayCommand]
        private void NuevaApertura()
        {
            MensajeError = null;
            ResultadoUltimoCierre = null;
            FechaApertura = null;
            FondoInicialActual = 0m;
            _sesionCajaActualId = null;
            LimpiarMovimientosCajaActual();
            EstadoVista = EstadoVistaCaja.Apertura;
        }

        [RelayCommand]
        private async Task ActualizarMovimientosCajaActualAsync()
        {
            MensajeError = null;

            try
            {
                var sesionAbierta = await _cajaRepository.ObtenerSesionAbiertaAsync();

                if (sesionAbierta is null)
                {
                    _sesionCajaActualId = null;
                    LimpiarMovimientosCajaActual();
                    MensajeError = "No existe una sesión de caja abierta.";
                    return;
                }

                _sesionCajaActualId = sesionAbierta.Id;
                FechaApertura = sesionAbierta.FechaApertura;
                FondoInicialActual = sesionAbierta.FondoInicial;
                await CargarMovimientosCajaActualAsync();
            }
            catch (ArgumentException ex) { MensajeError = ex.Message; }
        }

        [RelayCommand]
        private void LimpiarFiltrosMovimientosActuales()
        {
            TipoMovimientoActualFiltroSeleccionado = "Todos";
            MetodoPagoActualFiltroSeleccionado = "Todos";
        }

        private async Task CargarMovimientosCajaActualAsync()
        {
            MovimientosCajaActual.Clear();

            if (!_sesionCajaActualId.HasValue)
            {
                ActualizarResumenCajaActual();
                return;
            }

            var movimientos = await _consultarDetalleMovimientosCajaUseCase.EjecutarAsync(_sesionCajaActualId.Value);
            foreach (var movimiento in movimientos) MovimientosCajaActual.Add(movimiento);

            MovimientosCajaActualVista.Refresh();
            ActualizarResumenCajaActual();
        }

        private void LimpiarMovimientosCajaActual()
        {
            MovimientosCajaActual.Clear();
            TipoMovimientoActualFiltroSeleccionado = "Todos";
            MetodoPagoActualFiltroSeleccionado = "Todos";
            ActualizarResumenCajaActual();
        }
        private void ActualizarResumenCajaActual()
        {
            IngresosEfectivoActual = MovimientosCajaActual
                .Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == MetodoPago.Efectivo)
                .Sum(m => m.Monto);

            EgresosEfectivoActual = MovimientosCajaActual
                .Where(m => m.Tipo == TipoMovimientoCaja.EgresoAbastecimiento && m.MetodoPago == MetodoPago.Efectivo)
                .Sum(m => m.Monto);

            ReversionesEfectivoActual = MovimientosCajaActual
                .Where(m => m.Tipo == TipoMovimientoCaja.ReversionVenta && m.MetodoPago == MetodoPago.Efectivo)
                .Sum(m => m.Monto);

            EfectivoEsperadoActual = FondoInicialActual + IngresosEfectivoActual - EgresosEfectivoActual - ReversionesEfectivoActual;
            YapeNetoActual = CalcularNetoMetodoPago(MetodoPago.Yape);
            PlinNetoActual = CalcularNetoMetodoPago(MetodoPago.Plin);
        }
        private decimal CalcularNetoMetodoPago(MetodoPago metodoPago)
        {
            var ingresos = MovimientosCajaActual
                .Where(m => m.Tipo == TipoMovimientoCaja.IngresoVenta && m.MetodoPago == metodoPago)
                .Sum(m => m.Monto);

            var egresos = MovimientosCajaActual
                .Where(m => m.Tipo == TipoMovimientoCaja.EgresoAbastecimiento && m.MetodoPago == metodoPago)
                .Sum(m => m.Monto);

            var reversiones = MovimientosCajaActual
                .Where(m => m.Tipo == TipoMovimientoCaja.ReversionVenta && m.MetodoPago == metodoPago)
                .Sum(m => m.Monto);

            return ingresos - egresos - reversiones;
        }
        private bool FiltrarMovimientoCajaActual(object item)
        {
            if (item is not MovimientoCajaDetalleResult movimiento) return false;

            if (TipoMovimientoActualFiltroSeleccionado == "Ingresos" && movimiento.Tipo != TipoMovimientoCaja.IngresoVenta) return false;
            if (TipoMovimientoActualFiltroSeleccionado == "Egresos" && movimiento.Tipo != TipoMovimientoCaja.EgresoAbastecimiento) return false;
            if (TipoMovimientoActualFiltroSeleccionado == "Reversiones" && movimiento.Tipo != TipoMovimientoCaja.ReversionVenta) return false;
            if (MetodoPagoActualFiltroSeleccionado != "Todos" && movimiento.MetodoPago.ToString() != MetodoPagoActualFiltroSeleccionado) return false;

            return true;
        }

        [RelayCommand]
        private async Task AbrirHistorialAsync()
        {
            MensajeError = null;
            CierreSeleccionado = null;
            MostrarDetalleCierre = false;
            MostrarResolucionDescuadre = false;
            await CargarUsuariosFiltroAsync();
            await CargarHistorialAsync();
            _estadoAnteriorHistorial = EstadoVista;
            EstadoVista = EstadoVistaCaja.Historial;
        }
        private async Task CargarUsuariosFiltroAsync()
        {
            UsuariosFiltro.Clear();
            UsuariosFiltro.Add(new UsuarioFiltroCajaItem { Id = null, Nombre = "Todos" });

            var usuarioActual = _sesionUsuario.UsuarioActual;

            if (usuarioActual?.EsAdministradora == true)
            {
                var usuarios = await _listarUsuariosUseCase.EjecutarAsync();

                foreach (var usuario in usuarios)
                    UsuariosFiltro.Add(new UsuarioFiltroCajaItem { Id = usuario.Id, Nombre = usuario.NombreCompleto });
            }
            else if (usuarioActual is not null)
            {
                UsuariosFiltro.Add(new UsuarioFiltroCajaItem
                {
                    Id = usuarioActual.IdUsuario,
                    Nombre = usuarioActual.NombreCompleto
                });
            }

            UsuarioFiltroSeleccionado = UsuariosFiltro.FirstOrDefault();
        }
        private async Task CargarHistorialAsync()
        {
            var cierres = await _consultarHistorialCierresUseCase.EjecutarAsync();
            var nombresUsuarios = UsuariosFiltro
                .Where(x => x.Id.HasValue)
                .ToDictionary(x => x.Id!.Value, x => x.Nombre);

            HistorialCierres.Clear();

            foreach (var cierre in cierres)
            {
                var nombreUsuario = nombresUsuarios.TryGetValue(cierre.UsuarioAperturaId, out var nombre)
                    ? nombre
                    : $"Usuario #{cierre.UsuarioAperturaId}";

                HistorialCierres.Add(new HistorialCierreItem
                {
                    Id = cierre.Id,
                    UsuarioAperturaId = cierre.UsuarioAperturaId,
                    UsuarioAperturaNombre = nombreUsuario,
                    FechaApertura = cierre.FechaApertura,
                    FechaCierre = cierre.FechaCierre,
                    FondoInicial = cierre.FondoInicial,
                    EfectivoEsperado = cierre.EfectivoEsperado,
                    EfectivoReal = cierre.EfectivoReal,
                    DiferenciaEfectivo = cierre.DiferenciaEfectivo,
                    YapeNeto = cierre.YapeEsperado,
                    PlinNeto = cierre.PlinEsperado,
                    ObservacionCierre = cierre.ObservacionCierre,
                    DescuadreResuelto = cierre.DescuadreResuelto,
                    FechaResolucionDescuadre = cierre.FechaResolucionDescuadre,
                    UsuarioResolucionId = cierre.UsuarioResolucionId,
                    ObservacionResolucionDescuadre = cierre.ObservacionResolucionDescuadre
                });
            }
            HistorialCierresVista.Refresh();
            ActualizarCantidadCierresMostrados();
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
            MostrarDetalleCierre = false;
            MostrarResolucionDescuadre = false;
            CierreSeleccionado = null;
            EstadoVista = _estadoAnteriorHistorial;
        }

        [RelayCommand]
        private void AplicarFiltrosHistorial()
        {
            MensajeError = null;

            if (FechaDesdeFiltro.HasValue && FechaHastaFiltro.HasValue &&
                FechaDesdeFiltro.Value.Date > FechaHastaFiltro.Value.Date)
            {
                MensajeError = "La fecha desde no puede ser posterior a la fecha hasta.";
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

            if (montoMinimo.HasValue && montoMaximo.HasValue && montoMinimo.Value > montoMaximo.Value)
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
            UsuarioFiltroSeleccionado = UsuariosFiltro.FirstOrDefault();
            EstadoDescuadreFiltroSeleccionado = "Todos";
            HistorialCierresVista.Refresh();
            ActualizarCantidadCierresMostrados();
        }

        [RelayCommand]
        private async Task VerDetalleCierreAsync(HistorialCierreItem? cierre)
        {
            if (cierre is null) return;
            MensajeError = null;

            try
            {
                var movimientos = await _consultarDetalleMovimientosCajaUseCase.EjecutarAsync(cierre.Id);
                MovimientosCierreSeleccionado.Clear();
                foreach (var movimiento in movimientos) MovimientosCierreSeleccionado.Add(movimiento);
                CierreSeleccionado = cierre;
                MostrarDetalleCierre = true;
            }
            catch (ArgumentException ex) { MensajeError = ex.Message; }
        }

        [RelayCommand]
        private void CerrarDetalleCierre()
        {
            MostrarDetalleCierre = false;
            CierreSeleccionado = null;
            MovimientosCierreSeleccionado.Clear();
        }

        [RelayCommand]
        private void AbrirResolucionDescuadre()
        {
            MensajeError = null;

            if (!PuedeResolverDescuadre)
            {
                MensajeError = "El cierre seleccionado no tiene un descuadre pendiente que pueda resolverse.";
                return;
            }

            ObservacionResolucionDescuadreInput = null;
            MostrarResolucionDescuadre = true;
        }

        [RelayCommand]
        private void CancelarResolucionDescuadre()
        {
            MostrarResolucionDescuadre = false;
            ObservacionResolucionDescuadreInput = null;
        }
        [RelayCommand]
        private async Task ConfirmarResolucionDescuadreAsync()
        {
            if (CierreSeleccionado is null) return;
            MensajeError = null;

            try
            {
                var cierreId = CierreSeleccionado.Id;
                await _resolverDescuadreCajaUseCase.EjecutarAsync(cierreId, ObservacionResolucionDescuadreInput);
                MostrarResolucionDescuadre = false;
                ObservacionResolucionDescuadreInput = null;
                await CargarHistorialAsync();
                CierreSeleccionado = HistorialCierres.FirstOrDefault(x => x.Id == cierreId);
            }
            catch (ArgumentException ex) { MensajeError = ex.Message; }
            catch (InvalidOperationException ex) { MensajeError = ex.Message; }
            catch (UnauthorizedAccessException ex) { MensajeError = ex.Message; }
        }

        private static bool TryParseMonto(string texto, out decimal monto)
        {
            var textoNormalizado = texto.Trim().Replace(',', '.');

            return decimal.TryParse(
                textoNormalizado,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out monto);
        }
        private bool FiltrarCierre(object item)
        {
            if (item is not HistorialCierreItem cierre) return false;

            if (FechaDesdeFiltro.HasValue && cierre.FechaCierre.HasValue &&
                cierre.FechaCierre.Value.Date < FechaDesdeFiltro.Value.Date) return false;

            if (FechaHastaFiltro.HasValue && cierre.FechaCierre.HasValue &&
                cierre.FechaCierre.Value.Date > FechaHastaFiltro.Value.Date) return false;

            if (_montoMinimoAplicado.HasValue &&
                (!cierre.EfectivoReal.HasValue || cierre.EfectivoReal.Value < _montoMinimoAplicado.Value)) return false;

            if (_montoMaximoAplicado.HasValue &&
                (!cierre.EfectivoReal.HasValue || cierre.EfectivoReal.Value > _montoMaximoAplicado.Value)) return false;

            if (UsuarioFiltroSeleccionado?.Id is int usuarioId && cierre.UsuarioAperturaId != usuarioId) return false;

            if (EstadoDescuadreFiltroSeleccionado != "Todos" &&
                cierre.EstadoDescuadre != EstadoDescuadreFiltroSeleccionado) return false;

            return true;
        }

        private void ActualizarCantidadCierresMostrados() =>
            CantidadCierresMostrados = HistorialCierresVista.Cast<object>().Count();
    }
}
