using BodegaLuchito.Domain.Autenticacion.Entities;
using BodegaLuchito.Domain.Shared.Enums;
using System;
using System.Collections.Generic;

namespace BodegaLuchito.Domain.Ventas.Entities;

public sealed class Venta
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public DateTime FechaHora { get; set; } = DateTime.Now;

    // Importes
    public decimal Subtotal { get; set; }
    public decimal IGV { get; set; }
    public decimal Total { get; set; }

    // Medio de pago (Efectivo, Yape, Plin)
    public MetodoPago MetodoPago { get; set; }

    // Regla RN-10: Las ventas no se eliminan, se anulan.
    public bool Anulado { get; set; } = false;
    public string? MotivoAnulacion { get; set; }
    public DateTime? FechaAnulacion { get; set; }
    public int? UsuarioAnulacionId { get; set; }

    public List<DetalleVenta> Detalles { get; set; } = new();
}
