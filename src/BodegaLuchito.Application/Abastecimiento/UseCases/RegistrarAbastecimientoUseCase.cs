using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Abastecimiento.DTOs;
using BodegaLuchito.Application.Abastecimiento.Interfaces;
using BodegaLuchito.Domain.Abastecimiento.Entities;
using BodegaLuchito.Application.Productos.UseCases;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Application.Abastecimiento.UseCases;

public class RegistrarAbastecimientoUseCase
{
    private readonly IAbastecimientoRepository _abastecimientoRepository;

    public RegistrarAbastecimientoUseCase(
        IAbastecimientoRepository abastecimientoRepository)
    {
        _abastecimientoRepository = abastecimientoRepository;
    }

    public async Task ExecuteAsync(RegistrarAbastecimientoRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Detalles == null || !request.Detalles.Any())
            throw new ArgumentException("El abastecimiento debe tener al menos un producto.");

        if (request.ProveedorId <= 0)
            throw new ArgumentException("Debe seleccionar un proveedor válido.");

        if (request.UsuarioId <= 0)
            throw new ArgumentException("Debe existir un usuario autenticado para registrar el abastecimiento.");

        if (request.Detalles.Any(d =>
            (d.ProductoNuevo is null ? d.ProductoId <= 0 : d.ProductoId != 0) ||
            d.Cantidad <= 0 ||
            decimal.Round(d.Cantidad, 3) != d.Cantidad ||
            d.CostoUnitario <= 0 ||
            d.TotalLinea <= 0))
        {
            throw new ArgumentException(
                "Cada producto debe tener cantidad, costo unitario y total de línea mayores a cero.");
        }

        if (request.Detalles.Where(d => d.ProductoNuevo is null).GroupBy(d => d.ProductoId).Any(group => group.Count() > 1))
            throw new ArgumentException("Un producto solo puede aparecer una vez en el abastecimiento.");

        var totalCalculado = Math.Round(
            request.Detalles.Sum(d => d.TotalLinea),
            2,
            MidpointRounding.AwayFromZero);

        if (totalCalculado <= 0)
        {
            throw new ArgumentException(
                "El total del abastecimiento debe ser mayor a cero.");
        }
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
                Producto = det.ProductoNuevo is null ? null! : PrepararProductoNuevo.Crear(det.ProductoNuevo),
                Cantidad = det.Cantidad,
                CostoUnitario = det.CostoUnitario,
                TotalLinea = det.TotalLinea
            });
        }

        await _abastecimientoRepository.RegistrarOperacionAsync(
            abastecimiento,
            request.UsuarioId,
            cancellationToken);
    }
}
