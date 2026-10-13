using System;

namespace BodegaLuchito.Application.Reportes.DTOs;

public record ReporteCierreCajaDto(
    DateTime FechaApertura,
    DateTime? FechaCierre,
    string UsuarioApertura,
    decimal FondoInicial,
    decimal EfectivoEsperado,
    decimal EfectivoReal,
    decimal DiferenciaEfectivo,
    string Estado);
