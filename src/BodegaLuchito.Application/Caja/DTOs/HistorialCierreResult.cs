using BodegaLuchito.Domain.Caja.Entities;

namespace BodegaLuchito.Application.Caja.DTOs;

public sealed record HistorialCierreResult(SesionCaja Sesion, string AbiertoPor, string CerradoPor);
