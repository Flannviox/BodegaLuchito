using System.Collections.Generic;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Application.Abastecimiento.DTOs;

public class DetalleAbastecimientoRequest
{
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal Subtotal => Cantidad * PrecioUnitario;
}

public class RegistrarAbastecimientoRequest
{
    public int ProveedorId { get; set; }
    public decimal Total { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public int UsuarioId { get; set; } // Necesario para registrar quién hizo el movimiento en caja

    public List<DetalleAbastecimientoRequest> Detalles { get; set; } = new();
}
