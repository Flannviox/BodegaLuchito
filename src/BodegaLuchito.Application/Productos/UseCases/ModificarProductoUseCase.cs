using System;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Application.Productos.UseCases;

public sealed class ModificarProductoUseCase
{
    private readonly IProductoRepository _productoRepository;

    public ModificarProductoUseCase(IProductoRepository productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task EjecutarAsync(ModificarProductoRequest request, CancellationToken cancellationToken = default)
    {
        //Validaciones base
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre del producto no puede estar vacío.", nameof(request.Nombre));

        if (request.CategoriaId <= 0)
            throw new ArgumentException("La categoría es obligatoria.", nameof(request.CategoriaId));

        if (request.PrecioVenta <= 0)
            throw new ArgumentException("El precio de venta debe ser mayor a cero.", nameof(request.PrecioVenta));

        if (request.StockActual < 0)
            throw new ArgumentException("El stock actual no puede ser negativo.", nameof(request.StockActual));

        if (request.StockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(request.StockMinimo));

        if (request.UnidadVenta == UnidadVenta.Unidad)
        {
            if (request.StockActual % 1 != 0)
                throw new ArgumentException("El stock actual debe ser un número entero para productos por unidad.", nameof(request.StockActual));

            if (request.StockMinimo % 1 != 0)
                throw new ArgumentException("El stock mínimo debe ser un número entero para productos por unidad.", nameof(request.StockMinimo));
        }

        //Verificar que el producto existe
        var producto = await _productoRepository.ObtenerPorIdAsync(request.Id, cancellationToken);
        if (producto == null)
            throw new InvalidOperationException("El producto que intenta modificar no existe en el sistema.");

        //Normalizar strings
        var nombreLimpio = request.Nombre.Trim();
        var codigoBarrasLimpio = string.IsNullOrWhiteSpace(request.CodigoBarras) ? null : request.CodigoBarras.Trim();

        //Validar código de barras único (Solo si el usuario lo cambió)
        if (codigoBarrasLimpio != null && codigoBarrasLimpio != producto.CodigoBarras)
        {
            var existeCodigo = await _productoRepository.ExisteCodigoBarrasAsync(codigoBarrasLimpio, cancellationToken);
            if (existeCodigo)
                throw new InvalidOperationException("El código de barras ya está registrado en otro producto.");
        }

        //Actualizar la entidad
        producto.Nombre = nombreLimpio;
        producto.CategoriaId = request.CategoriaId;
        producto.CodigoBarras = codigoBarrasLimpio;
        producto.PrecioVenta = request.PrecioVenta;
        producto.UnidadVenta = request.UnidadVenta;
        producto.ControlaInventario = request.ControlaInventario;

        if (producto.ControlaInventario)
        {
            producto.StockActual = request.StockActual;
            producto.StockMinimo = request.StockMinimo;
        }
        else
        {
            producto.StockActual = 0;
            producto.StockMinimo = 0;
        }

        //Guardar cambios
        await _productoRepository.ActualizarAsync(producto, cancellationToken);
    }
}
