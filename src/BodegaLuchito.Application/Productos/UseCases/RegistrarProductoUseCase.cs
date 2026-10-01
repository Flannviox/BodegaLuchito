using System;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Application.Productos.UseCases;

public class RegistrarProductoUseCase
{
    private readonly IProductoRepository _productoRepository;

    public RegistrarProductoUseCase(IProductoRepository productoRepository)
    {
        _productoRepository = productoRepository;
    }

    public async Task EjecutarAsync(RegistrarProductoRequest request, CancellationToken cancellationToken = default)
    {
        var nombre = request.Nombre?.Trim();
        var codigoBarras = string.IsNullOrWhiteSpace(request.CodigoBarras) ? null : request.CodigoBarras.Trim();

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del producto es obligatorio.");

        // Validamos que se haya enviado un ID de categoría válido
        if (request.CategoriaId <= 0)
            throw new ArgumentException("La categoría es obligatoria.");

        if (request.PrecioVenta <= 0)
            throw new ArgumentException("El precio de venta debe ser mayor a cero.");

        const decimal stockActual = 0;
        const bool controlaInventario = true;
        var stockMinimo = request.StockMinimo;

        if (stockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.");

        if (request.UnidadVenta == UnidadVenta.Unidad)
        {
            if (stockMinimo % 1 != 0)
                throw new ArgumentException("El stock mínimo debe ser un número entero para productos por unidad.", nameof(request.StockMinimo));
        }

        if (codigoBarras != null)
        {
            bool existe = await _productoRepository.ExisteCodigoBarrasAsync(codigoBarras, cancellationToken);
            if (existe)
                throw new InvalidOperationException($"El código de barras '{codigoBarras}' ya se encuentra registrado.");
        }

        var producto = new Producto
        {
            Nombre = nombre,
            CategoriaId = request.CategoriaId, // Asignamos el ID de la categoría
            CodigoBarras = codigoBarras,
            PrecioVenta = request.PrecioVenta,
            UnidadVenta = request.UnidadVenta,
            ControlaInventario = controlaInventario,
            StockActual = stockActual,
            StockMinimo = stockMinimo,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        await _productoRepository.AgregarAsync(producto, cancellationToken);
    }
}
