using System;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;

namespace BodegaLuchito.Application.Productos.UseCases;

public sealed class ReactivarProductoUseCase
{
    private readonly IProductoRepository _productoRepository;
    private readonly ISesionUsuario _sesionUsuario;

    public ReactivarProductoUseCase(
        IProductoRepository productoRepository,
        ISesionUsuario sesionUsuario)
    {
        _productoRepository = productoRepository;
        _sesionUsuario = sesionUsuario;
    }

    public async Task EjecutarAsync(int id, CancellationToken cancellationToken = default)
    {
        var producto = await _productoRepository.ObtenerPorIdAsync(id, cancellationToken);

        if (producto is null)
            throw new InvalidOperationException("El producto que intenta reactivar no existe en el sistema.");

        if (producto.Activo)
            throw new InvalidOperationException("El producto ya se encuentra activo.");

        var usuarioActual = _sesionUsuario.UsuarioActual;
        if (usuarioActual is null)
            throw new InvalidOperationException("Debe iniciar sesión para reactivar un producto.");

        producto.Activo = true;

        var historialActividad = new HistorialActividadProducto
        {
            ProductoId = producto.Id,
            Tipo = TipoActividadProducto.Reactivacion,
            Descripcion = "Producto reactivado en el catálogo.",
            FechaHora = DateTime.Now,
            UsuarioNombre = usuarioActual.NombreCompleto
        };

        await _productoRepository.ActualizarEstadoConHistorialAsync(
            producto,
            historialActividad,
            cancellationToken);
    }
}
