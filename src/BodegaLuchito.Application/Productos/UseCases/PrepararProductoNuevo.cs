using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Application.Productos.UseCases;

// Reglas compartidas por el catálogo y las altas dentro de un ingreso.
public static class PrepararProductoNuevo
{
    public static Producto Crear(RegistrarProductoRequest datos)
    {
        var nombre = datos.Nombre?.Trim();
        var codigo = string.IsNullOrWhiteSpace(datos.CodigoBarras) ? null : datos.CodigoBarras.Trim();
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Length > 150)
            throw new ArgumentException("Escribe un nombre de hasta 150 caracteres.");
        if (datos.CategoriaId <= 0) throw new ArgumentException("Selecciona una categoría.");
        if (codigo?.Length > 50) throw new ArgumentException("El código admite hasta 50 caracteres.");
        if (!Enum.IsDefined(datos.UnidadVenta)) throw new ArgumentException("Selecciona Unidad o Peso.");
        if (datos.PrecioVenta <= 0) throw new ArgumentException("El precio de venta debe ser mayor a cero.");
        if (datos.StockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.");
        if (datos.UnidadVenta == UnidadVenta.Unidad && datos.StockMinimo % 1 != 0)
            throw new ArgumentException("El stock mínimo debe ser entero para productos por unidad.");
        return new Producto {
            Nombre = nombre, CategoriaId = datos.CategoriaId, CodigoBarras = codigo,
            PrecioVenta = datos.PrecioVenta, UnidadVenta = datos.UnidadVenta,
            StockMinimo = datos.StockMinimo, StockActual = 0, ControlaInventario = true
        };
    }
}
