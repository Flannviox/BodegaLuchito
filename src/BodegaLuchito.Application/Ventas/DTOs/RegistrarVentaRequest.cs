using System.Collections.Generic;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Application.Ventas.DTOs;

public class DetalleVentaRequest
{
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal { get; set; }
}

public class RegistrarVentaRequest
{
    public int UsuarioId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal IGV { get; set; }
    public decimal Total { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public List<DetalleVentaRequest> Detalles { get; set; } = new();
}
