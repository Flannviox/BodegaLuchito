namespace BodegaLuchito.Application.BI.DTOs;

public record ResumenMedioPagoDto(
    string MedioPago,
    decimal TotalMonto,
    int CantidadTransacciones,
    decimal PorcentajeDelTotal);
