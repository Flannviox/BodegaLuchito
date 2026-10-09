namespace BodegaLuchito.Application.BI;

public sealed record VentaDiaria(DateTime Fecha, decimal Cantidad);
public sealed record HistorialDemanda(int ProductoId, string Producto, string Unidad,
    decimal Stock, DateTime Desde, IReadOnlyList<VentaDiaria> Ventas);

public interface IHistorialDemandaRepository
{
    Task<IReadOnlyList<HistorialDemanda>> ConsultarAsync(DateTime desde, DateTime hastaExclusiva,
        CancellationToken cancellationToken = default);
}

public interface IModeloDemanda
{
    decimal[] Predecir(IReadOnlyList<decimal> cantidades, int horizonte);
}

public sealed record ResultadoPrediccion(int ProductoId, string Producto, string Unidad, decimal Stock,
    int Dias, string Estado, decimal? Demanda, string Riesgo, decimal? ErrorModelo,
    decimal? ErrorPromedio, string Detalle, int PeriodosEvaluados = 0);

public sealed record InformePrediccion(bool Demostracion, DateTime CalculadoEn, DateTime Desde,
    DateTime Hasta, IReadOnlyList<ResultadoPrediccion> Productos);
