using System.Collections.Generic;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Application.Abastecimiento.DTOs;

public class DetalleAbastecimientoRequest
{
    public int ProductoId { get; set; }
    public BodegaLuchito.Application.Productos.DTOs.RegistrarProductoRequest? ProductoNuevo { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal TotalLinea { get; set; }
}

public class RegistrarAbastecimientoRequest
{
    public int ProveedorId { get; set; }
    public MetodoPago MetodoPago { get; set; }
    public int UsuarioId { get; set; }

    public List<DetalleAbastecimientoRequest> Detalles { get; set; } = new();
}
