namespace BodegaLuchito.Application.BI;

public sealed record VentaDiaria(DateTime Fecha, decimal Cantidad);
public sealed record HistorialDemanda(int ProductoId, string Producto, string Unidad,
    decimal Stock, DateTime Desde, IReadOnlyList<VentaDiaria> Ventas, bool ControlaInventario = true);

public interface IHistorialDemandaRepository
{
    Task<IReadOnlyList<HistorialDemanda>> ConsultarAsync(DateTime desde, DateTime hastaExclusiva,
        CancellationToken cancellationToken = default);
}

public interface IModeloDemanda
{
    decimal[] Predecir(IReadOnlyList<decimal> cantidades, int horizonte);
}

/// <summary>
/// Resultado semanal de un producto vendido en la semana analizada.
/// VendidoSemana: lo vendido realmente en la semana cerrada (lunes a domingo).
/// Demanda: estimación del modelo para los siete días siguientes.
/// VentaReal: lo vendido realmente en la semana estimada, solo si esa semana ya terminó.
/// ErrorSemanaAnterior: error de la referencia simple "repetir lo vendido la semana previa".
/// </summary>
public sealed record ResultadoPrediccion(int ProductoId, string Producto, string Unidad, decimal Stock,
    bool ControlaInventario, int Dias, string Estado, decimal VendidoSemana, decimal? VendidoSemanaAnterior,
    decimal? Demanda, decimal? VentaReal, string Riesgo, decimal? ErrorModelo, decimal? ErrorSemanaAnterior,
    string Detalle, int SemanasEvaluadas = 0);

public sealed record InformePrediccion(bool Demostracion, DateTime CalculadoEn, DateTime SemanaInicio,
    DateTime SemanaFin, DateTime ProyeccionInicio, DateTime ProyeccionFin, bool ProyeccionTerminada,
    int ProductosSinVentas, IReadOnlyList<ResultadoPrediccion> Productos);
