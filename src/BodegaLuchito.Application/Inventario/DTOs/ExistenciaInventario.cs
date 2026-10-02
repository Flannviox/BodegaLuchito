using BodegaLuchito.Domain.Productos.Enums;
namespace BodegaLuchito.Application.Inventario.DTOs;

public sealed record ExistenciaInventario(int ProductoId, string Nombre, string? Codigo,
    string Categoria, UnidadVenta UnidadVenta, decimal StockActual, decimal StockMinimo,
    bool Activo, bool PuedeRegistrarSaldoInicial)
{
    public string Unidad => UnidadVenta == UnidadVenta.Peso ? "kg" : "unid";
    public string Estado => !Activo ? "Desactivado" : StockActual == 0 ? "Agotado"
        : StockMinimo > 0 && StockActual <= StockMinimo ? "Stock bajo" : "Disponible";
}
