using System.Collections.ObjectModel;
using BodegaLuchito.Application.BI;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.BI.ViewModels;

/// <summary>Semana cerrada (lunes a domingo) que se puede elegir para revisar.</summary>
public sealed record SemanaOpcion(DateTime Inicio, bool EsUltima)
{
    public DateTime Fin => Inicio.AddDays(6);
    public override string ToString() => EsUltima
        ? $"La semana pasada ({Inicio:dd/MM} al {Fin:dd/MM})"
        : $"Del {Inicio:dd/MM} al {Fin:dd/MM}";
}

/// <summary>Recuadro de resumen superior (número grande y texto corto).</summary>
public sealed record IndicadorResumen(string Valor, string Texto, string Nivel);

public partial class PrediccionDemandaViewModel(GenerarPrediccionDemandaUseCase generar) : ViewModelBase
{
    private CancellationTokenSource? _cancelacion;
    public string[] Fuentes { get; } = ["Mis ventas", "Un ejemplo"];
    public IReadOnlyList<SemanaOpcion> Semanas { get; } = CrearSemanas();
    public ObservableCollection<ProductoPrediccionViewModel> Resultados { get; } = [];
    public ObservableCollection<IndicadorResumen> Indicadores { get; } = [];
    public DateTime FechaMinima => DateTime.Today.AddDays(-GenerarPrediccionDemandaUseCase.MaximoDias);
    public DateTime FechaMaxima => DateTime.Today.AddDays(-1);

    [ObservableProperty] private string _fuente = "Mis ventas";
    [ObservableProperty] private SemanaOpcion? _semana = CrearSemanas()[0];
    [ObservableProperty] private DateTime? _desde = DateTime.Today.AddDays(-90);
    [ObservableProperty] private bool _historialCompleto;
    [ObservableProperty] private bool _ocupado;
    [ObservableProperty] private string _mensaje = "Elige la semana y pulsa «Revisar».";
    [ObservableProperty] private string _fechaCalculo = "";
    [ObservableProperty] private string _periodo = "";
    [ObservableProperty] private bool _analisisRealizado;
    [ObservableProperty] private ProductoPrediccionViewModel? _seleccionado;
    [ObservableProperty] private bool _proyeccionTerminada;

    public bool Disponible => !Ocupado;
    public bool EsNegocio => Fuente == Fuentes[0];
    public bool EsEjemplo => !EsNegocio;
    public bool TieneResultados => Resultados.Count > 0;
    public bool MostrarBienvenida => !AnalisisRealizado && !Ocupado;
    public bool HaySeleccion => Seleccionado is not null;
    public string TituloApoyo => ProyeccionTerminada ? "Se vendió en realidad" : "Tienes ahora";

    private static SemanaOpcion[] CrearSemanas() => GenerarPrediccionDemandaUseCase.SemanasCerradas(DateTime.Today)
        .Select((inicio, i) => new SemanaOpcion(inicio, i == 0)).ToArray();

    partial void OnFuenteChanged(string value)
    {
        HistorialCompleto = false;
        OnPropertyChanged(nameof(EsNegocio));
        OnPropertyChanged(nameof(EsEjemplo));
        LimpiarResultado();
    }
    partial void OnSemanaChanged(SemanaOpcion? value) => LimpiarResultado();
    partial void OnDesdeChanged(DateTime? value) => LimpiarResultado();
    partial void OnHistorialCompletoChanged(bool value) => LimpiarResultado();
    partial void OnOcupadoChanged(bool value)
    {
        OnPropertyChanged(nameof(Disponible));
        OnPropertyChanged(nameof(MostrarBienvenida));
    }
    partial void OnAnalisisRealizadoChanged(bool value)
    {
        OnPropertyChanged(nameof(MostrarBienvenida));
        OnPropertyChanged(nameof(TieneResultados));
    }
    partial void OnSeleccionadoChanged(ProductoPrediccionViewModel? value) => OnPropertyChanged(nameof(HaySeleccion));
    partial void OnProyeccionTerminadaChanged(bool value) => OnPropertyChanged(nameof(TituloApoyo));

    private void LimpiarResultado()
    {
        Resultados.Clear();
        Indicadores.Clear();
        AnalisisRealizado = false;
        Seleccionado = null;
        Periodo = "";
        FechaCalculo = "";
        Mensaje = "Elige la semana y pulsa «Revisar».";
        OnPropertyChanged(nameof(TieneResultados));
    }

    [RelayCommand]
    private async Task AnalizarAsync()
    {
        if (Ocupado) return;
        if (EsNegocio && Desde is null) { Mensaje = "Elige desde qué fecha registras tus ventas (en «Más opciones»)."; return; }
        if (EsNegocio && Semana is null) { Mensaje = "Elige la semana que quieres revisar."; return; }
        LimpiarResultado();
        Ocupado = true;
        _cancelacion = new CancellationTokenSource();
        Mensaje = "Revisando tus ventas… espera un momento.";
        try
        {
            var informe = await generar.EjecutarAsync(!EsNegocio, Semana?.Inicio ?? Semanas[0].Inicio,
                Desde ?? DateTime.Today.AddDays(-90), HistorialCompleto, _cancelacion.Token);
            foreach (var fila in informe.Productos)
                Resultados.Add(new ProductoPrediccionViewModel(fila, informe.ProyeccionTerminada));
            CrearIndicadores(informe.ProyeccionTerminada);
            ProyeccionTerminada = informe.ProyeccionTerminada;
            Seleccionado = Resultados.FirstOrDefault();
            AnalisisRealizado = true;
            OnPropertyChanged(nameof(TieneResultados));
            Periodo = informe.ProyeccionTerminada
                ? $"Ventas del {informe.SemanaInicio:dd/MM} al {informe.SemanaFin:dd/MM}  →  comparado con lo que se vendió del {informe.ProyeccionInicio:dd/MM} al {informe.ProyeccionFin:dd/MM}"
                : $"Ventas del {informe.SemanaInicio:dd/MM} al {informe.SemanaFin:dd/MM}  →  cálculo para la semana del {informe.ProyeccionInicio:dd/MM} al {informe.ProyeccionFin:dd/MM}";
            FechaCalculo = $"Revisado el {informe.CalculadoEn:dd/MM/yyyy} a las {informe.CalculadoEn:HH:mm}."
                + (informe.ProductosSinVentas > 0 ? $" No se muestran {informe.ProductosSinVentas} producto(s) que no se vendieron esa semana." : "");
            Mensaje = Resultados.Count == 0 ? "No hubo ventas registradas en esa semana. Prueba con otra semana."
                : informe.Demostracion ? "Esto es un ejemplo con productos inventados. Tus ventas y tu stock no cambian."
                : informe.ProyeccionTerminada ? "Esa semana ya pasó: puedes ver si el cálculo acertó."
                : Resultados.All(x => x.Resultado.Demanda is null) ? "Todavía faltan ventas registradas para calcular. Lee cada tarjeta para saber qué falta."
                : "Listo. Los productos que necesitan atención aparecen primero. Toca uno para ver qué hacer.";
        }
        catch (OperationCanceledException) { Mensaje = "Se canceló la revisión. Puedes intentarlo de nuevo."; }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            Mensaje = ex is ArgumentException or InvalidOperationException ? ex.Message
                : "No se pudo revisar. Cierra y vuelve a abrir esta pantalla; si sigue igual, pide ayuda.";
        }
        finally
        {
            _cancelacion.Dispose();
            _cancelacion = null;
            Ocupado = false;
        }
    }

    private void CrearIndicadores(bool proyeccionTerminada)
    {
        Indicadores.Clear();
        if (Resultados.Count == 0) return;
        int Contar(string nivel) => Resultados.Count(x => x.TieneEstimacion && x.Nivel == nivel);
        Indicadores.Add(new(Resultados.Count.ToString(), "productos vendidos esa semana", "Neutro"));
        if (proyeccionTerminada)
        {
            Indicadores.Add(new(Contar("Bajo").ToString(), "acertó", "Bajo"));
            Indicadores.Add(new(Contar("Medio").ToString(), "estuvo cerca", "Medio"));
            Indicadores.Add(new(Contar("Alto").ToString(), "se alejó", "Alto"));
        }
        else
        {
            Indicadores.Add(new(Contar("Alto").ToString(), "podrían faltar", "Alto"));
            Indicadores.Add(new(Contar("Medio").ToString(), "quedan pocos", "Medio"));
            Indicadores.Add(new(Contar("Bajo").ToString(), "te alcanzan", "Bajo"));
        }
        var sinCalculo = Resultados.Count(x => !x.TieneEstimacion);
        if (sinCalculo > 0) Indicadores.Add(new(sinCalculo.ToString(), "aún sin cálculo", "Neutro"));
    }

    [RelayCommand]
    private void Cancelar() => _cancelacion?.Cancel();
}
