using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BodegaLuchito.Application.BI.UseCases;
using BodegaLuchito.Application.BI.DTOs;
using BodegaLuchito.Application.Inventario.DTOs;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;

namespace BodegaLuchito.Desktop.Modules.BI.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly ObtenerDatosDashboardUseCase _dashboardUseCase;

    [ObservableProperty]
    private DashboardResult? _datos;

    [ObservableProperty]
    private bool _estaCargando;

    // --- Propiedades para los Gráficos de LiveCharts2 ---
    [ObservableProperty]
    private ObservableCollection<ISeries> _serieEvolucionVentas = new();

    [ObservableProperty]
    private ObservableCollection<Axis> _ejeX = new();

    [ObservableProperty]
    private ObservableCollection<Axis> _ejeY = new();

    [ObservableProperty]
    private ObservableCollection<ISeries> _serieMediosPago = new();

    public DashboardViewModel(ObtenerDatosDashboardUseCase dashboardUseCase)
    {
        _dashboardUseCase = dashboardUseCase;
        ConfigurarEjesGrafico();
    }

    // Este método se llamará cuando la vista se cargue
    public async Task InicializarAsync()
    {
        await CargarDashboardAsync();
    }

    [RelayCommand]
    private async Task CargarDashboardAsync()
    {
        if (EstaCargando) return;

        try
        {
            EstaCargando = true;

            // 1. Obtener la data cruda del caso de uso
            Datos = await _dashboardUseCase.EjecutarAsync();

            // 2. Dibujar el Gráfico de Líneas (Evolución de Ventas)
            DibujarGraficoLineas();

            // 3. Dibujar el Gráfico de Torta (Medios de Pago)
            DibujarGraficoTorta();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ocurrió un error al cargar el panel de indicadores: {ex.Message}", "Error de Dashboard", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            EstaCargando = false;
        }
    }

    private void ConfigurarEjesGrafico()
    {
        // Configuramos la apariencia base de los ejes del gráfico de líneas
        EjeY = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labeler = value => $"S/ {value:N0}",
                TextSize = 12,
                MinLimit = 0
            }
        };
    }

    private void DibujarGraficoLineas()
    {
        if (Datos?.EvolucionVentasUltimos7Dias == null) return;

        var valores = Datos.EvolucionVentasUltimos7Dias.Select(v => (double)v.TotalMonto).ToArray();
        var etiquetasX = Datos.EvolucionVentasUltimos7Dias.Select(v => v.Fecha.ToString("dd/MM")).ToArray();

        SerieEvolucionVentas = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Values = valores,
                Name = "Ventas (S/)",
                Fill = new SolidColorPaint(SKColors.Blue.WithAlpha(30)),
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                GeometrySize = 10,
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
            }
        };

        EjeX = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labels = etiquetasX,
                TextSize = 12
            }
        };
    }

    private void DibujarGraficoTorta()
    {
        if (Datos?.MediosDePagoDelMes == null) return;

        var seriesTorta = new ObservableCollection<ISeries>();

        foreach (var medio in Datos.MediosDePagoDelMes)
        {
            // Asignamos colores según el medio de pago para mantener consistencia visual
            SKColor colorMedio = medio.MedioPago switch
            {
                "Efectivo" => SKColor.Parse("#10B981"), // Verde
                "Yape" => SKColor.Parse("#8B5CF6"),     // Morado
                "Plin" => SKColor.Parse("#0EA5E9"),     // Celeste
                _ => SKColor.Parse("#94A3B8")           // Gris por defecto
            };

            seriesTorta.Add(new PieSeries<double>
            {
                Values = new double[] { (double)medio.TotalMonto },
                Name = medio.MedioPago,
                Fill = new SolidColorPaint(colorMedio),
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{medio.MedioPago}\n{medio.PorcentajeDelTotal:N1}%"
            });
        }

        SerieMediosPago = seriesTorta;
    }
}
