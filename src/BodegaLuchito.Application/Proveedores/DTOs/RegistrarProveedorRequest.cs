namespace BodegaLuchito.Application.Proveedores.DTOs;

public class RegistrarProveedorRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string? Ruc { get; set; }
    public string? Telefono { get; set; }
    public string? Direccion { get; set; }
}
