using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Application.Productos.UseCases;

public class RegistrarProductoUseCase
{
    private readonly IProductoRepository _productoRepository;

    public RegistrarProductoUseCase(IProductoRepository productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task EjecutarAsync(RegistrarProductoRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre del producto es obligatorio.");

        if (string.IsNullOrWhiteSpace(request.Categoria))
            throw new ArgumentException("La categoría es obligatoria.");

        if (request.PrecioVenta <= 0)
            throw new ArgumentException("El precio de venta debe ser mayor a cero.");

        decimal stockActual = request.StockActual;
        decimal stockMinimo = request.StockMinimo;
        string? codigoBarras = request.CodigoBarras;

        // Regla: Si no controla inventario, stocks van a 0
        if (!request.ControlaInventario)
        {
            stockActual = 0;
            stockMinimo = 0;
        }
        else
        {
            if (stockActual < 0) throw new ArgumentException("El stock actual no puede ser negativo.");
            if (stockMinimo < 0) throw new ArgumentException("El stock mínimo no puede ser negativo.");
        }

        // Regla: Validar código duplicado solo si se ha ingresado uno
        if (!string.IsNullOrWhiteSpace(codigoBarras))
        {
            bool existe = await _productoRepository.ExisteCodigoBarrasAsync(codigoBarras);
            if (existe)
                throw new InvalidOperationException($"El código de barras '{codigoBarras}' ya se encuentra registrado.");
        }

        var producto = new Producto
        {
            Nombre = request.Nombre,
            Categoria = request.Categoria,
            CodigoBarras = string.IsNullOrWhiteSpace(codigoBarras) ? null : codigoBarras,
            PrecioVenta = request.PrecioVenta,
            UnidadVenta = request.UnidadVenta,
            ControlaInventario = request.ControlaInventario,
            StockActual = stockActual,
            StockMinimo = stockMinimo,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        await _productoRepository.AgregarAsync(producto);
    }
}
