using BodegaLuchito.Application.BI;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace BodegaLuchito.Infrastructure.BI;

public sealed class ModeloDemandaSsa : IModeloDemanda
{
    public decimal[] Predecir(IReadOnlyList<decimal> cantidades, int horizonte)
    {
        if (cantidades.Count < GenerarPrediccionDemandaUseCase.MinimoDiasEntrenamiento)
            throw new ArgumentException("SSA requiere al menos 12 días de entrenamiento en esta versión.");
        if (horizonte <= 0 || cantidades.Any(x => x < 0))
            throw new ArgumentException("El horizonte debe ser positivo y las cantidades no negativas.");
        // Ventana corta para el piloto; no pretende aprender estacionalidad semanal con dos semanas.
        var ventana = cantidades.Count < 30 ? 3 : 7;
        var longitud = cantidades.Count < 30 ? Math.Min(14, cantidades.Count) : 30;
        var ml = new MLContext(seed: 42);
        var datos = ml.Data.LoadFromEnumerable(cantidades.Select(x => new Entrada { Cantidad = (float)x }));
        var estimador = ml.Forecasting.ForecastBySsa(nameof(Salida.Pronostico), nameof(Entrada.Cantidad),
            windowSize: ventana, seriesLength: longitud, trainSize: cantidades.Count, horizon: horizonte);
        var modelo = estimador.Fit(datos);
        using var motor = modelo.CreateTimeSeriesEngine<Entrada, Salida>(ml);
        var valores = motor.Predict().Pronostico;
        if (valores.Length != horizonte || valores.Any(x => !float.IsFinite(x)))
            throw new InvalidOperationException("SSA devolvió un resultado no finito.");
        return valores.Select(x => (decimal)Math.Max(0, x)).ToArray();
    }

    public sealed class Entrada { public float Cantidad { get; set; } }
    public sealed class Salida { public float[] Pronostico { get; set; } = []; }
}
