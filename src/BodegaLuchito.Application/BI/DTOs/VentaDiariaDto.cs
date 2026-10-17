using System;

namespace BodegaLuchito.Application.BI.DTOs;

public record VentaDiariaDto(
    DateTime Fecha,
    decimal TotalMonto);
