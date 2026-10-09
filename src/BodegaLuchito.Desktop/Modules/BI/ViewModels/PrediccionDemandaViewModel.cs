using System.Collections.ObjectModel;
using BodegaLuchito.Application.BI;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.BI.ViewModels;

public partial class PrediccionDemandaViewModel(GenerarPrediccionDemandaUseCase generar) : ViewModelBase
{
    private CancellationTokenSource? _cancelacion;
    public string[] Fuentes { get; } = ["Mi negocio", "Ver un ejemplo"];
    public ObservableCollection<ProductoPrediccionViewModel> Resultados { get; } = [];
    public DateTime FechaMinima => DateTime.Today.AddDays(-GenerarPrediccionDemandaUseCase.MaximoDias);
    public DateTime FechaMaxima => DateTime.Today.AddDays(-1);

    [ObservableProperty] private string _fuente = "Mi negocio";
    [ObservableProperty] private DateTime? _desde = DateTime.Today.AddDays(-90);
    [ObservableProperty] private bool _historialCompleto;
    [ObservableProperty] private bool _ocupado;
    [ObservableProperty] private string _mensaje = "Puedes usar tus ventas o ver un ejemplo sin cambiar tu negocio.";
    [ObservableProperty] private string _fechaCalculo = "Todavía no se ha realizado una consulta.";
    [ObservableProperty] private bool _analisisRealizado;
    [ObservableProperty] private bool _detallesVisibles;
    [ObservableProperty] private ProductoPrediccionViewModel? _seleccionado;

    public bool Disponible => !Ocupado;
    public bool EsNegocio => Fuente == Fuentes[0];
    public string AvisoOrigen => EsNegocio
        ? "Se usan las ventas que guardaste en el sistema. Si son ventas de prueba, esta consulta también será de prueba."
        : "ESTÁS VIENDO UN EJEMPLO · Estos productos y cantidades son inventados. Tu inventario y tu caja no cambian.";
    public string Resumen => !AnalisisRealizado ? "Productos que conviene revisar"
        : $"Productos revisados: {Resultados.Count} · Podrían faltar: {Resultados.Count(x => x.Riesgo == "Alto")} · "
        + $"Aún sin cálculo: {Resultados.Count(x => x.Resultado.Demanda is null)}";
    public string TituloDetalle => Seleccionado is null ? "¿Cómo leer esta pantalla?" : $"Sobre {Seleccionado.Producto}";
    public string DetalleSeleccion => Seleccionado?.Explicacion ?? "Compara lo que tienes con lo que podrías vender. Selecciona un producto para entender su resultado.";
    public string AdvertenciaSeleccion => Seleccionado?.Advertencia ?? "";
    public bool TieneAdvertencia => !string.IsNullOrEmpty(AdvertenciaSeleccion);
    public string DetalleTecnico => Seleccionado?.Resultado.Detalle ?? "Selecciona un producto para consultar los detalles del cálculo.";
    public string Evaluacion => Seleccionado?.Resultado is { ErrorModelo: decimal error } fila
        ? $"Historial: {fila.Dias} días. Evaluación: {fila.PeriodosEvaluados} período(s) de 3 días. Error medio: modelo {error:0.###} {fila.Unidad} · promedio {fila.ErrorPromedio:0.###} {fila.Unidad}. Menor es mejor. Estimación sin redondeo visual: {fila.Demanda:0.###} {fila.Unidad}."
        : "Todavía no hay una estimación para evaluar.";

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
    partial void OnAnalisisRealizadoChanged(bool value) => OnPropertyChanged(nameof(Resumen));
    partial void OnSeleccionadoChanged(ProductoPrediccionViewModel? value)
    {
        OnPropertyChanged(nameof(TituloDetalle));
        OnPropertyChanged(nameof(DetalleSeleccion));
        OnPropertyChanged(nameof(AdvertenciaSeleccion));
        OnPropertyChanged(nameof(TieneAdvertencia));
        OnPropertyChanged(nameof(DetalleTecnico));
        OnPropertyChanged(nameof(Evaluacion));
    }

    private void LimpiarResultado()
    {
        Resultados.Clear();
        AnalisisRealizado = false;
        DetallesVisibles = false;
        Seleccionado = null;
        FechaCalculo = "Todavía no se ha realizado una consulta.";
        Mensaje = "Pulsa Revisar productos para consultar con estos datos.";
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
        Mensaje = "Revisando tus productos y las ventas anteriores…";
        try
        {
            var informe = await generar.EjecutarAsync(!EsNegocio, Desde ?? DateTime.Today.AddDays(-90),
                HistorialCompleto, _cancelacion.Token);
            foreach (var fila in informe.Productos) Resultados.Add(new ProductoPrediccionViewModel(fila));
            AnalisisRealizado = true;
            Seleccionado = Resultados.FirstOrDefault();
            FechaCalculo = $"Consulta: {informe.CalculadoEn:dd/MM/yyyy HH:mm} · Para las ventas del {informe.CalculadoEn:dd/MM} al {informe.CalculadoEn.AddDays(2):dd/MM}.";
            Mensaje = Resultados.Count == 0 ? "No hay productos activos con inventario para revisar."
                : informe.Demostracion ? "Ejemplo listo. Selecciona un producto para ver qué significa su resultado."
                : Resultados.All(x => x.Resultado.Demanda is null) ? "Todavía no podemos calcular las ventas próximas. Selecciona un producto para saber qué falta."
                : "Consulta lista. Vuelve a revisar después de registrar nuevas ventas o recibir mercadería.";
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
