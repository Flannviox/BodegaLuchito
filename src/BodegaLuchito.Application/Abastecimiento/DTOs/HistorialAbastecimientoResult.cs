using BodegaLuchito.Domain.Abastecimiento.Entities;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Application.Abastecimiento.DTOs;

public sealed class HistorialAbastecimientoResult
{
    public int Id { get; init; }
    public DateTime FechaHora { get; init; }
    public Proveedor Proveedor { get; init; } = null!;
    public MetodoPago MetodoPago { get; init; }
    public decimal Total { get; init; }
    public IReadOnlyList<DetalleAbastecimiento> Detalles { get; init; } = [];
    public string RegistradoPor { get; init; } = "Sin registro de responsable";
}
