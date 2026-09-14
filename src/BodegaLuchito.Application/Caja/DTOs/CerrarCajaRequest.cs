using System;
using System.Collections.Generic;
using System.Text;

namespace BodegaLuchito.Application.Caja.DTOs
{
    public record CerrarCajaRequest(

        int UsuarioCierreId,
        decimal EfectivoReal,
        decimal YapeReal,
        decimal PlinReal,
        string? ObservacionCierre
    );
}
