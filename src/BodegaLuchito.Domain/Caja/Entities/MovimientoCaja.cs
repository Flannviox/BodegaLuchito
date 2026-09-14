using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Shared.Enums;

namespace BodegaLuchito.Domain.Caja.Entities
{
    public class MovimientoCaja
    {
        public int Id { get; set; }

        public int SesionCajaId { get; set; }
        public SesionCaja? SesionCaja { get; set; }

        public int UsuarioId { get; set; }
        public TipoMovimientoCaja Tipo { get; set; }
        public MetodoPago MetodoPago { get; set; }
        public decimal Monto { get; set; }
        public DateTime FechaHora { get; set; }

        public int? VentaId { get; set; }
        public int? AbastecimientoId { get; set; }
        public string? Descripcion { get; set; }
    }
}
