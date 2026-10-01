using System;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Application.Productos.UseCases;

public sealed class ModificarProductoUseCase
{
    private readonly IProductoRepository _productoRepository;
    private readonly ISesionUsuario _sesionUsuario;

    public ModificarProductoUseCase(
        IProductoRepository productoRepository,
        ISesionUsuario sesionUsuario)
    {
        _productoRepository = productoRepository;
        _sesionUsuario = sesionUsuario;
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

        if (request.StockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(request.StockMinimo));

        if (request.UnidadVenta == UnidadVenta.Unidad)
        {
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
        producto.Categoria = null!; // Desvinculamos el objeto viejo para que EF Core obedezca al nuevo CategoriaId
        producto.CodigoBarras = codigoBarrasLimpio;
        var precioAnterior = producto.PrecioVenta;
        producto.PrecioVenta = request.PrecioVenta;
        producto.UnidadVenta = request.UnidadVenta;
        producto.ControlaInventario = true;
        producto.StockMinimo = request.StockMinimo;

        HistorialPrecioProducto? historialPrecio = null;
        if (precioAnterior != producto.PrecioVenta)
        {
            historialPrecio = new HistorialPrecioProducto
            {
                ProductoId = producto.Id,
                PrecioAnterior = precioAnterior,
                PrecioNuevo = producto.PrecioVenta,
                FechaHora = DateTime.Now,
                UsuarioNombre = _sesionUsuario.UsuarioActual?.NombreCompleto ?? "Sistema"
            };
        }

        await _productoRepository.ActualizarConHistorialPrecioAsync(
            producto,
            historialPrecio,
            cancellationToken);
    }
}
