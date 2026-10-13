using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
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

    // Filtros de Fecha
    [ObservableProperty]
    private DateTime _fechaInicio = DateTime.Today.AddDays(-30); // Por defecto el último mes

    [ObservableProperty]
    private DateTime _fechaFin = DateTime.Today;

    // Selector de Tipo de Reporte
    public ObservableCollection<string> TiposReporte { get; } = new()
    {
        "Ventas",
        "Abastecimientos",
        "Movimientos de Caja"
    };

    [ObservableProperty]
    private string _tipoReporteSeleccionado = "Ventas";

    // Resultados dinámicos (Se bindean al DataGrid de la Vista)
    [ObservableProperty]
    private IEnumerable<object> _resultados = new List<object>();

    // Tarjetas de Resumen Superior
    [ObservableProperty]
    private decimal _totalVentas;

    [ObservableProperty]
    private decimal _totalEgresos;

    [ObservableProperty]
    private decimal _diferenciaCaja;

    public ReportesViewModel(ObtenerReportesUseCase obtenerReportesUseCase, ExportacionExcelService excelService)
    {
        _obtenerReportesUseCase = obtenerReportesUseCase;
        _excelService = excelService;
    }

    [RelayCommand]
    private async Task GenerarReporteAsync()
    {
        try
        {
            if (TipoReporteSeleccionado == "Ventas")
            {
                var data = await _obtenerReportesUseCase.ObtenerReporteVentasAsync(FechaInicio, FechaFin);
                Resultados = data;
                TotalVentas = data.Sum(x => x.Total);
            }
            else if (TipoReporteSeleccionado == "Abastecimientos")
            {
                var data = await _obtenerReportesUseCase.ObtenerReporteAbastecimientosAsync(FechaInicio, FechaFin);
                Resultados = data;
                TotalEgresos = data.Sum(x => x.Total);
            }
            else if (TipoReporteSeleccionado == "Movimientos de Caja")
            {
                var data = await _obtenerReportesUseCase.ObtenerReporteCajaAsync(FechaInicio, FechaFin);
                Resultados = data;
                DiferenciaCaja = TotalVentas - TotalEgresos; // Cálculo referencial
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al consultar los datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void LimpiarFiltros()
    {
        FechaInicio = DateTime.Today.AddDays(-30);
        FechaFin = DateTime.Today;
        TipoReporteSeleccionado = "Ventas";
        Resultados = new List<object>();
        TotalVentas = 0;
        TotalEgresos = 0;
        DiferenciaCaja = 0;
    }

    [RelayCommand]
    private void ExportarExcel()
    {
        if (Resultados == null || !Resultados.Any())
        {
            MessageBox.Show("No hay datos para exportar. Por favor, genere un reporte primero.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            // Ruta donde se guardará el Excel (Por defecto el Escritorio de la computadora)
            string rutaEscritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string carpetaDestino = System.IO.Path.Combine(rutaEscritorio, "Reportes_BodegaLuchito");

            // LLamamos a nuestro servicio mágico genérico de ClosedXML
            string rutaFinal = _excelService.ExportarAExcel(Resultados, TipoReporteSeleccionado, carpetaDestino);

            MessageBox.Show($"Reporte exportado exitosamente.\nSe guardó en:\n{rutaFinal}", "Exportación Exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocurrió un error al crear el archivo Excel: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
