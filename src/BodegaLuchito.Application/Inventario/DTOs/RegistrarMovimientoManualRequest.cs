using BodegaLuchito.Domain.Inventario.Enums;
namespace BodegaLuchito.Application.Inventario.DTOs;

// En conteo se recibe el total contado; en merma, la cantidad que se retira.
public sealed record RegistrarMovimientoManualRequest(int ProductoId, TipoMovimientoInventario Tipo,
    decimal Cantidad, decimal StockEsperado, string Motivo,
    BodegaLuchito.Application.Productos.DTOs.RegistrarProductoRequest? ProductoNuevo = null);
