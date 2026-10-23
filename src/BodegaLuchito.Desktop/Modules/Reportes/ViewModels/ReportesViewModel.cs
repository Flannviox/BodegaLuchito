using System.Diagnostics;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows;
using BodegaLuchito.Application.Reportes.DTOs;
using BodegaLuchito.Application.Reportes.UseCases;
using BodegaLuchito.Application.Reportes.Servicios;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Reportes.ViewModels;

public partial class ReportesViewModel : ViewModelBase
{
    private readonly ObtenerReportesUseCase _obtenerReportesUseCase;
    private readonly ExportacionExcelService _excelService;
    private ResultadoReporte? _reporteGenerado;
    private int _versionFiltros;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HayAvisoExportacion))]
    private string _mensajeExportacion = string.Empty;
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AbrirCarpetaCommand))]
    private string? _ultimoArchivoExportado;
    public bool HayAvisoExportacion => !string.IsNullOrEmpty(MensajeExportacion);

    private bool PuedeAbrirCarpeta() => !string.IsNullOrEmpty(UltimoArchivoExportado);

    [RelayCommand(CanExecute = nameof(PuedeAbrirCarpeta))]
    private void AbrirCarpeta()
    {
        try
        {
            var carpeta = Path.GetDirectoryName(UltimoArchivoExportado);
            if (string.IsNullOrEmpty(carpeta) || !Directory.Exists(carpeta))
                throw new IOException("La carpeta ya no está disponible.");
            Process.Start(new ProcessStartInfo(carpeta) { UseShellExecute = true });
        }
        catch (Exception ex) { MensajeExportacion = $"No se pudo abrir la carpeta: {ex.Message}"; }
    }

    [RelayCommand]
    private void CerrarAvisoExportacion() => MensajeExportacion = string.Empty;

    [ObservableProperty] private DateTime _fechaInicio = DateTime.Today.AddDays(-30);
    [ObservableProperty] private DateTime _fechaFin = DateTime.Today;
    [ObservableProperty] private string _tipoReporteSeleccionado = "Ventas";
    public ObservableCollection<string> TiposReporte { get; } = new()
    {
        "Ventas", "Abastecimientos", "Movimientos de Caja"
    };

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportarExcelCommand))]
    private IReadOnlyList<object> _resultados = Array.Empty<object>();
    [ObservableProperty] private decimal _totalVentas;
    [ObservableProperty] private decimal _totalEgresos;
    [ObservableProperty] private decimal _diferenciaCaja;
    [ObservableProperty] private string _mensajeEstado = "Seleccione el período y genere un reporte.";
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ExportarExcelCommand))]
    [NotifyCanExecuteChangedFor(nameof(GenerarReporteCommand))]
    private bool _estaGenerando;

    public ReportesViewModel(ObtenerReportesUseCase obtenerReportesUseCase, ExportacionExcelService excelService)
    {
        _obtenerReportesUseCase = obtenerReportesUseCase;
        _excelService = excelService;
    }

    partial void OnFechaInicioChanged(DateTime value) => InvalidarReporte();
    partial void OnFechaFinChanged(DateTime value) => InvalidarReporte();
    partial void OnTipoReporteSeleccionadoChanged(string value) => InvalidarReporte();

    private void InvalidarReporte()
    {
        _versionFiltros++;
        _reporteGenerado = null;
        Resultados = Array.Empty<object>();
        TotalVentas = TotalEgresos = DiferenciaCaja = 0;
        MensajeEstado = "Filtros actualizados. Genere el reporte para consultar y exportar.";
    }

    private bool PuedeGenerar() => !EstaGenerando;
    private bool PuedeExportar() => !EstaGenerando && _reporteGenerado is not null && Resultados.Count > 0;

    [RelayCommand(CanExecute = nameof(PuedeGenerar))]
    private async Task GenerarReporteAsync()
    {
        if (EstaGenerando) return;
        InvalidarReporte();
        var version = _versionFiltros;
        var tipo = TipoReporteSeleccionado;
        var inicio = FechaInicio;
        var fin = FechaFin;
        try
        {
            EstaGenerando = true;
            MensajeEstado = "Consultando información…";
            var reporte = await _obtenerReportesUseCase.GenerarAsync(tipo, inicio, fin);
            // Una consulta anterior no debe reemplazar filtros editados mientras se consultaba.
            if (version != _versionFiltros) return;
            _reporteGenerado = reporte;
            Resultados = reporte.Filas;
            TotalVentas = reporte.TotalVentas;
            TotalEgresos = reporte.TotalEgresos;
            DiferenciaCaja = reporte.DiferenciaCierres;
            MensajeEstado = $"{reporte.Tipo} · {reporte.Inicio:dd/MM/yyyy} al {reporte.Fin:dd/MM/yyyy} · {reporte.Filas.Count} registros · {reporte.CantidadCierres} cierres de caja.";
        }
        catch (Exception ex)
        {
            if (version == _versionFiltros) MensajeEstado = $"No se generó el reporte: {ex.Message}";
        }
        finally { EstaGenerando = false; }
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        FechaInicio = DateTime.Today.AddDays(-30);
        FechaFin = DateTime.Today;
        TipoReporteSeleccionado = "Ventas";
        InvalidarReporte();
    }

    [RelayCommand(CanExecute = nameof(PuedeExportar))]
    private void ExportarExcel()
    {
        var reporte = _reporteGenerado;
        if (!PuedeExportar() || reporte is null) return;
        try
        {
            var carpetaDestino = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Reportes_BodegaLuchito");
            var rutaFinal = _excelService.ExportarAExcel(reporte.Filas, reporte.Tipo, carpetaDestino,
                reporte.Inicio, reporte.Fin);
            UltimoArchivoExportado = rutaFinal;
            MensajeExportacion = "Excel guardado. Tu reporte está listo para usar.";
        }
        catch (Exception ex)
        {
            UltimoArchivoExportado = null;
            MensajeExportacion = $"No se pudo guardar el reporte: {ex.Message}";
        }
    }
}
