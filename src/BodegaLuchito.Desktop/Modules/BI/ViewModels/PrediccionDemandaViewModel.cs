using System.Collections.ObjectModel;
using BodegaLuchito.Application.BI;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.BI.ViewModels;

public partial class PrediccionDemandaViewModel(GenerarPrediccionDemandaUseCase generar) : ViewModelBase
{
    private CancellationTokenSource? _cancelacion;
    public string[] Fuentes { get; } = ["Datos del negocio", "Demostración"];
    public ObservableCollection<ResultadoPrediccion> Resultados { get; } = [];
    public DateTime FechaMinima => DateTime.Today.AddDays(-GenerarPrediccionDemandaUseCase.MaximoDias);
    public DateTime FechaMaxima => DateTime.Today.AddDays(-1);

    [ObservableProperty] private string _fuente = "Datos del negocio";
    [ObservableProperty] private DateTime? _desde = DateTime.Today.AddDays(-90);
    [ObservableProperty] private bool _historialCompleto;
    [ObservableProperty] private bool _ocupado;
    [ObservableProperty] private string _mensaje = "Selecciona el origen de los datos y pulsa Analizar demanda.";
    [ObservableProperty] private string _fechaCalculo = "Sin análisis";
    [ObservableProperty] private ResultadoPrediccion? _seleccionado;

    public bool Disponible => !Ocupado;
    public bool EsNegocio => Fuente == Fuentes[0];
    public string AvisoOrigen => EsNegocio
        ? "Desde 15 días completos puedes probar una estimación experimental. Si registras ventas de prueba, los resultados también serán de prueba."
        : "DEMOSTRACIÓN · Productos, ventas y stock ficticios, separados de la base del negocio. No es necesario registrar ventas para probarla.";
    public string Resumen => $"{Resultados.Count} productos · {Resultados.Count(x => x.Demanda.HasValue)} estimaciones · "
        + $"{Resultados.Count(x => x.Riesgo == "Alto")} con riesgo alto";
    public string DetalleSeleccion => Seleccionado?.Detalle ?? "Selecciona un producto para ver su evaluación.";
    public string Evaluacion => Seleccionado?.ErrorModelo is decimal error
        ? $"Evaluación: {Seleccionado.PeriodosEvaluados} período(s) de 3 días. Error medio: modelo {error:0.###} {Seleccionado.Unidad} · promedio {Seleccionado.ErrorPromedio:0.###} {Seleccionado.Unidad}. Menor es mejor."
        : "La evaluación aparecerá cuando exista una estimación.";

    partial void OnFuenteChanged(string value)
    {
        HistorialCompleto = false;
        OnPropertyChanged(nameof(EsNegocio));
        OnPropertyChanged(nameof(AvisoOrigen));
        LimpiarResultado();
    }
    partial void OnDesdeChanged(DateTime? value) => LimpiarResultado();
    partial void OnHistorialCompletoChanged(bool value) => LimpiarResultado();
    partial void OnOcupadoChanged(bool value) => OnPropertyChanged(nameof(Disponible));
    partial void OnSeleccionadoChanged(ResultadoPrediccion? value)
    {
        OnPropertyChanged(nameof(DetalleSeleccion));
        OnPropertyChanged(nameof(Evaluacion));
    }

    private void LimpiarResultado()
    {
        Resultados.Clear();
        Seleccionado = null;
        FechaCalculo = "Sin análisis";
        Mensaje = "Pulsa Analizar demanda para calcular con estos datos.";
        OnPropertyChanged(nameof(Resumen));
    }

    [RelayCommand]
    private async Task AnalizarAsync()
    {
        if (Ocupado) return;
        if (EsNegocio && Desde is null) { Mensaje = "Selecciona desde cuándo están completas las ventas."; return; }
        LimpiarResultado();
        Ocupado = true;
        _cancelacion = new CancellationTokenSource();
        Mensaje = "Analizando el historial y evaluando las predicciones…";
        try
        {
            var informe = await generar.EjecutarAsync(!EsNegocio, Desde ?? DateTime.Today.AddDays(-90),
                HistorialCompleto, _cancelacion.Token);
            foreach (var fila in informe.Productos) Resultados.Add(fila);
            Seleccionado = Resultados.FirstOrDefault();
            FechaCalculo = $"Calculado: {informe.CalculadoEn:dd/MM/yyyy HH:mm} · Ventas hasta {informe.Hasta:dd/MM/yyyy}";
            Mensaje = Resultados.Count == 0 ? "No hay productos activos con inventario para analizar."
                : informe.Demostracion ? "Demostración calculada. Su desempeño no demuestra precisión en el negocio real."
                : "Análisis terminado. Las existencias corresponden al momento de la consulta; vuelve a analizar después de nuevas operaciones.";
        }
        catch (OperationCanceledException) { Mensaje = "Análisis cancelado. Puedes volver a intentarlo."; }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            Mensaje = ex is ArgumentException or InvalidOperationException ? ex.Message
                : "No se pudo completar el análisis. Revisa la base de datos y la instalación del módulo e inténtalo nuevamente.";
        }
        finally
        {
            _cancelacion.Dispose();
            _cancelacion = null;
            Ocupado = false;
            OnPropertyChanged(nameof(Resumen));
        }
    }

    [RelayCommand]
    private void Cancelar() => _cancelacion?.Cancel();
}
