namespace BodegaLuchito.Application.Ventas.DTOs;

public record AnularVentaRequest(
    int VentaId,
    string Motivo,
    int UsuarioAnulacionId
);
