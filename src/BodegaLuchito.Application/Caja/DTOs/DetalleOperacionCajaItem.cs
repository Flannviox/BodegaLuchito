using System;
using System.Collections.Generic;
using System.Text;

namespace BodegaLuchito.Application.Caja.DTOs
{
    public sealed record DetalleOperacionCajaItem(
     int ProductoId,
     string ProductoNombre,
     decimal Cantidad,
     decimal PrecioUnitario,
     decimal Subtotal);
}
