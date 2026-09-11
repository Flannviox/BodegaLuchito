using System;
using System.Collections.Generic;
using System.Text;

namespace BodegaLuchito.Application.Caja.DTOs
{
    public record AbrirCajaRequest(

        int UsuarioAperturaId,
        decimal FondoInicial

    );
}
