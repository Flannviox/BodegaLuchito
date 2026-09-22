using System;
using System.Collections.Generic;
using System.Text;

namespace BodegaLuchito.Application.Caja.DTOs
{
    public record CerrarCajaResult(

        decimal EfectivoEsperado,
        decimal EfectivoReal,
        decimal DiferenciaEfectivo,
        decimal YapeIngresos,
        decimal YapeSalidas,
        decimal YapeNeto,
        decimal PlinIngresos,
        decimal PlinSalidas,
        decimal PlinNeto

    );
}
