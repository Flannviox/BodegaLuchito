namespace BodegaLuchito.Application.BI.DTOs;

public record ResumenVentasDto(
    decimal TotalMonto,
    int CantidadTransacciones,
    decimal VariacionPorcentualMonto,
    decimal VariacionPorcentualTransacciones);
