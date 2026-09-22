using System;
using System.Collections.Generic;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Domain.Abastecimiento.Entities;

public class Abastecimiento
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public Proveedor Proveedor { get; set; } = null!;

    public DateTime FechaHora { get; set; } = DateTime.Now;
    public decimal Total { get; set; }
    public MetodoPago MetodoPago { get; set; }

    public List<DetalleAbastecimiento> Detalles { get; set; } = new();
}
