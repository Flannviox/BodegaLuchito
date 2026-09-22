using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Abastecimiento.DTOs;
using BodegaLuchito.Application.Abastecimiento.Interfaces;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Caja.Entities;
using BodegaLuchito.Domain.Caja.Enums;
using BodegaLuchito.Domain.Abastecimiento.Entities;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Application.Abastecimiento.UseCases;

public class RegistrarAbastecimientoUseCase
{
    private readonly IAbastecimientoRepository _abastecimientoRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly ICajaRepository _cajaRepository;

    public RegistrarAbastecimientoUseCase(
        IAbastecimientoRepository abastecimientoRepository,
        IProductoRepository productoRepository,
        ICajaRepository cajaRepository)
    {
        _abastecimientoRepository = abastecimientoRepository;
        _productoRepository = productoRepository;
        _cajaRepository = cajaRepository;
    }

    public async Task ExecuteAsync(RegistrarAbastecimientoRequest request, CancellationToken cancellationToken = default)
    {
        // 1. Validaciones básicas
        if (request.Detalles == null || !request.Detalles.Any())
            throw new ArgumentException("El abastecimiento debe tener al menos un producto.");

        if (request.Detalles.Any(d =>
            d.Cantidad <= 0 ||
            d.PrecioUnitario <= 0))
        {
            throw new ArgumentException(
                "La cantidad y el precio de los productos deben ser mayores a cero.");
        }

        var totalCalculado =
            request.Detalles.Sum(d => d.Subtotal);

        if (totalCalculado <= 0)
        {
            throw new ArgumentException(
                "El total del abastecimiento debe ser mayor a cero.");
        }
        var sesionAbierta = await _cajaRepository.ObtenerSesionAbiertaAsync(cancellationToken);

        if (sesionAbierta is null)
        {
            throw new InvalidOperationException("Debe abrir una sesión de caja antes de registrar un abastecimiento");
        }


        // 2. Preparar la entidad de Dominio
        var abastecimiento = new EntidadAbastecimiento
        {
            ProveedorId = request.ProveedorId,
            Total = totalCalculado,
            MetodoPago = request.MetodoPago,
            FechaHora = DateTime.Now
        };

        foreach (var det in request.Detalles)
        {
            abastecimiento.Detalles.Add(new DetalleAbastecimiento
            {
                ProductoId = det.ProductoId,
                Cantidad = det.Cantidad,
                PrecioUnitario = det.PrecioUnitario,
                Subtotal = det.Subtotal
            });
        }

        // 3. Guardar el abastecimiento (Cabecera y Detalles)
        await _abastecimientoRepository.RegistrarAsync(abastecimiento, cancellationToken);

        // 4. Actualizar el Inventario (Sumar stock)
        foreach (var det in request.Detalles)
        {
            var producto = await _productoRepository.ObtenerPorIdAsync(det.ProductoId, cancellationToken);
            if (producto != null && producto.ControlaInventario)
            {
                producto.StockActual += det.Cantidad;
                await _productoRepository.ActualizarAsync(producto, cancellationToken);
            }
        }

        // 5. Afectar la Caja
        var movimiento = new MovimientoCaja
        {
            SesionCajaId = sesionAbierta.Id,
            UsuarioId = request.UsuarioId,
            Tipo = TipoMovimientoCaja.EgresoAbastecimiento,
            MetodoPago = request.MetodoPago,
            Monto = totalCalculado,
            FechaHora = DateTime.Now,
            AbastecimientoId = abastecimiento.Id,
            Descripcion = $"Pago por abastecimiento #{abastecimiento.Id}"
        };

        await _cajaRepository.AgregarMovimientoAsync(movimiento, cancellationToken);
    }
}
