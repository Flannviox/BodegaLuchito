using BodegaLuchito.Domain.Inventario.Enums;
namespace BodegaLuchito.Application.Inventario.DTOs;

public sealed record MovimientoInventarioDetalle(int Id, int ProductoId, string Producto,
    DateTime Fecha, TipoMovimientoInventario Tipo, decimal Cantidad, decimal StockAnterior,
    decimal StockPosterior, string Unidad, string Responsable, string Motivo, int? AbastecimientoId)
{
    public string TipoDescripcion => Tipo switch
    {
        TipoMovimientoInventario.EntradaAbastecimiento => "Abastecimiento",
        TipoMovimientoInventario.SaldoInicial => "Saldo inicial",
        TipoMovimientoInventario.AjusteConteo => "Ajuste por conteo",
        TipoMovimientoInventario.Merma => "Pérdida o daño",
        _ => Tipo.ToString()
    };
    public decimal Variacion => StockPosterior - StockAnterior;
}
