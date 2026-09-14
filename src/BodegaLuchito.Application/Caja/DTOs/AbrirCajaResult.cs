using System;
using System.Collections.Generic;
using System.Text;

namespace BodegaLuchito.Application.Caja.DTOs
{
    public record AbrirCajaResult(

        int SesionCajaId,
        DateTime FechaApertura,
        decimal FondoInicial

    );

}
