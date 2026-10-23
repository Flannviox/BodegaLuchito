namespace BodegaLuchito.Application.Reportes.DTOs;

public sealed record ResultadoReporte(
    string Tipo,
    DateTime Inicio,
    DateTime Fin,
    IReadOnlyList<object> Filas,
    decimal TotalVentas,
    decimal TotalEgresos,
    decimal DiferenciaCierres,
    int CantidadCierres);
