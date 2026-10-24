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
    [NotifyPropertyChangedFor(nameof(MesSeleccionadoTexto))]
    [NotifyCanExecuteChangedFor(nameof(MesSiguienteCommand))]
    private DateTime _mesSeleccionado = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    public string MesSeleccionadoTexto => MesSeleccionado.ToString("MMMM 'de' yyyy", System.Globalization.CultureInfo.GetCultureInfo("es-PE"));
    public string TituloDia => Datos is null || Datos.FinPeriodo == default ? "Ventas de hoy" : $"Ventas del {Datos.FinPeriodo:dd/MM}";
    public string PeriodoTexto => Datos is null ? "Seleccione un mes para consultar." : $"{Datos.InicioPeriodo:dd/MM/yyyy} al {Datos.FinPeriodo:dd/MM/yyyy}";
    public string ComparacionMesTexto => MesSeleccionado.Year == DateTime.Today.Year && MesSeleccionado.Month == DateTime.Today.Month
        ? "vs. el mismo tramo del mes anterior" : "vs. mes anterior completo";

    private bool PuedeCambiarMes() => !EstaCargando;
    private bool PuedeAvanzarMes() => !EstaCargando && MesSeleccionado < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

    [RelayCommand(CanExecute = nameof(PuedeCambiarMes))]
    private async Task MesAnteriorAsync()
    {
        if (MesSeleccionado.Year == 1 && MesSeleccionado.Month <= 2) return;
        MesSeleccionado = MesSeleccionado.AddMonths(-1);
        await CargarDashboardAsync();
    }

    [RelayCommand(CanExecute = nameof(PuedeAvanzarMes))]
    private async Task MesSiguienteAsync()
    {
        MesSeleccionado = MesSeleccionado.AddMonths(1);
        await CargarDashboardAsync();
    }

    [RelayCommand(CanExecute = nameof(PuedeCambiarMes))]
    private async Task MesActualAsync()
    {
        MesSeleccionado = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        await CargarDashboardAsync();
    }

    [ObservableProperty] private ObservableCollection<ISeries> _serieCategorias = new();
    [ObservableProperty] private ObservableCollection<Axis> _ejeCategoriasX = new();
    [ObservableProperty] private ObservableCollection<Axis> _ejeCategoriasY = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VariacionDiaTexto))]
    [NotifyPropertyChangedFor(nameof(VariacionMesTexto))]
    [NotifyPropertyChangedFor(nameof(TituloDia))]
    [NotifyPropertyChangedFor(nameof(PeriodoTexto))]
    private DashboardResult? _datos;

    [ObservableProperty]
    private string _ultimaActualizacion = "Ventas, pagos e inventario en un solo lugar.";

    public string VariacionDiaTexto => Datos is null ? "Comparación con ayer" :
        $"{Datos.VentasDelDia.VariacionPorcentualMonto:+0.0;-0.0;0.0}% vs. el día anterior";

    public string VariacionMesTexto => Datos is null ? "Comparación con el mes anterior" :
        $"{Datos.VentasDelMes.VariacionPorcentualMonto:+0.0;-0.0;0.0}% {ComparacionMesTexto}";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MesAnteriorCommand))]
    [NotifyCanExecuteChangedFor(nameof(MesSiguienteCommand))]
    [NotifyCanExecuteChangedFor(nameof(MesActualCommand))]
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
            Datos = await _dashboardUseCase.EjecutarAsync(MesSeleccionado);

            // 2. Dibujar el Gráfico de Líneas (Evolución de Ventas)
            DibujarGraficoLineas();

            // 3. Dibujar el Gráfico de Torta (Medios de Pago)
            DibujarGraficoTorta();
            DibujarGraficoCategorias();
            UltimaActualizacion = $"Actualizado el {DateTime.Now:dd/MM/yyyy} a las {DateTime.Now:HH:mm} · ventas vigentes";
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
                TextSize = 11,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                MinLimit = 0,
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#EDF2F7"))
            }
        };
    }

    private void DibujarGraficoLineas()
    {
        if (Datos?.EvolucionVentasUltimos7Dias == null) return;

        var evolucion = Datos.EvolucionVentasDelMes.Count > 0 ? Datos.EvolucionVentasDelMes : Datos.EvolucionVentasUltimos7Dias;
        var valores = evolucion.Select(v => (double)v.TotalMonto).ToArray();
        var etiquetasX = evolucion.Select(v => v.Fecha.ToString("dd/MM")).ToArray();

        SerieEvolucionVentas = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Values = valores,
                Name = "Ventas (S/)",
                Fill = new SolidColorPaint(SKColor.Parse("#1767D8").WithAlpha(20)),
                Stroke = new SolidColorPaint(SKColor.Parse("#1767D8")) { StrokeThickness = 3 },
                GeometrySize = 6,
                LineSmoothness = 0,
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
            }
        };

        EjeX = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labeler = value =>
                {
                    var indice = (int)Math.Round(value);
                    return indice >= 0 && indice < etiquetasX.Length ? etiquetasX[indice] : string.Empty;
                },
                MinStep = Math.Max(1, (int)Math.Ceiling(etiquetasX.Length / 7d)),
                ForceStepToMin = true,
                TextSize = 11
            }
        };
    }

    private void DibujarGraficoCategorias()
    {
        if (Datos is null) return;
        var categorias = Datos.VentasPorCategoria.Take(6).Reverse().ToList();
        SerieCategorias = new ObservableCollection<ISeries>
        {
            new RowSeries<double>
            {
                Values = categorias.Select(c => (double)c.TotalMonto).ToArray(),
                Name = "Ventas por categoría", Fill = new SolidColorPaint(SKColor.Parse("#1767D8")),
                Stroke = null, MaxBarWidth = 24,
                XToolTipLabelFormatter = p => categorias[p.Index].Categoria,
                YToolTipLabelFormatter = p => $"S/ {p.Coordinate.PrimaryValue:N2}"
            }
        };
        EjeCategoriasY = new ObservableCollection<Axis>
        {
            new Axis { Labels = categorias.Select(c => c.Categoria).ToArray(), MinStep = 1, ForceStepToMin = true,
                TextSize = 12, LabelsPaint = new SolidColorPaint(SKColor.Parse("#52657E")) }
        };
        EjeCategoriasX = new ObservableCollection<Axis>
        {
            new Axis { MinLimit = 0, MinStep = Math.Max(1, Math.Ceiling(categorias.Select(c => (double)c.TotalMonto).DefaultIfEmpty(0).Max() / 3)), ForceStepToMin = true,
                Labeler = v => $"S/ {v:N0}", TextSize = 10,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#64748B")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#EDF2F7")) }
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
                "Efectivo" => SKColor.Parse("#078268"), // Verde
                "Yape" => SKColor.Parse("#8B5CF6"),     // Morado
                "Plin" => SKColor.Parse("#0EA5E9"),     // Celeste
                _ => SKColor.Parse("#94A3B8")           // Gris por defecto
            };

            seriesTorta.Add(new PieSeries<double>
            {
                Values = new double[] { (double)medio.TotalMonto },
                Name = medio.MedioPago,
                Fill = new SolidColorPaint(colorMedio),
                InnerRadius = 42,
                DataLabelsPaint = null
            });
        }

        SerieMediosPago = seriesTorta;
    }
}
