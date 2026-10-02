using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Ventas.DTOs;
using BodegaLuchito.Application.Ventas.Interfaces;
using BodegaLuchito.Domain.Ventas.Entities;

namespace BodegaLuchito.Application.Ventas.UseCases;

public class RegistrarVentaUseCase
{
    private readonly IVentaRepository _ventaRepository;

    public RegistrarVentaUseCase(IVentaRepository ventaRepository)
    {
        _ventaRepository = ventaRepository;
    }

    public async Task<int> ExecuteAsync(RegistrarVentaRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Detalles == null || !request.Detalles.Any())
            throw new ArgumentException("La venta debe tener al menos un producto.");

        if (request.UsuarioId <= 0)
            throw new ArgumentException("Debe existir un usuario autenticado para registrar la venta.");

        if (request.Total <= 0)
            throw new ArgumentException("El total de la venta debe ser mayor a cero.");

        // Construcción de la entidad Dominio
        var venta = new Venta
        {
            UsuarioId = request.UsuarioId,
            Subtotal = request.Subtotal,
            IGV = request.IGV,
            Total = request.Total,
            MetodoPago = request.MetodoPago,
            FechaHora = DateTime.Now,
            Anulado = false
        };

        foreach (var det in request.Detalles)
        {
            if (det.Cantidad <= 0 || det.PrecioUnitario <= 0)
                throw new ArgumentException("Las cantidades y precios deben ser mayores a cero.");

            venta.Detalles.Add(new DetalleVenta
            {
                ProductoId = det.ProductoId,
                Cantidad = det.Cantidad,
                PrecioUnitario = det.PrecioUnitario,
                Subtotal = det.Subtotal
            });
        }

        await _ventaRepository.RegistrarVentaAsync(venta, cancellationToken);

        return venta.Id;
    }
}
