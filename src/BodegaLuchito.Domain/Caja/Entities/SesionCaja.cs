using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Domain.Caja.Enums;

namespace BodegaLuchito.Domain.Caja.Entities
{
    public class SesionCaja
    {
        public int Id { get; set; }

        public int UsuarioAperturaId { get; set; }
        public DateTime FechaApertura { get; set; }
        public decimal FondoInicial { get; set; }
        public EstadoSesionCaja Estado { get; set; }

        public int? UsuarioCierreId { get; set; }
        public DateTime? FechaCierre { get; set; }

        public decimal? EfectivoEsperado { get; set; }
        public decimal? EfectivoReal { get; set; }
        public decimal? DiferenciaEfectivo { get; set; }

        public decimal? YapeEsperado { get; set; }
        public decimal? YapeReal { get; set; }
        public decimal? DiferenciaYape { get; set; }

        public decimal? PlinEsperado { get; set; }
        public decimal? PlinReal { get; set; }
        public decimal? DiferenciaPlin { get; set; }

        public string? ObservacionCierre { get; set; }

        public bool DescuadreResuelto { get; set; }

        public DateTime? FechaResolucionDescuadre { get; set; }

        public int? UsuarioResolucionId { get; set; }

        public string? ObservacionResolucionDescuadre { get; set; }

        public ICollection<MovimientoCaja> Movimientos { get; set; } = new List<MovimientoCaja>();


    }
}
