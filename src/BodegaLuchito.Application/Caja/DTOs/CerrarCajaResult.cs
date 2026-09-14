using System;
using System.Collections.Generic;
using System.Text;

namespace BodegaLuchito.Application.Caja.DTOs
{
    public record CerrarCajaResult(

        decimal EfectivoEsperado,
        decimal EfectivoReal,
        decimal DiferenciaEfectivo,
        decimal YapeEsperado,
        decimal YapeReal,
        decimal DiferenciaYape,
        decimal PlinEsperado,
        decimal PlinReal,
        decimal DiferenciaPlin

    );
}
