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
        var producto = PrepararProductoNuevo.Crear(request);
        if (producto.CodigoBarras is { } codigo &&
            await _productoRepository.ExisteCodigoBarrasAsync(codigo, cancellationToken))
            throw new InvalidOperationException($"El código de barras '{codigo}' ya se encuentra registrado.");
        await _productoRepository.AgregarAsync(producto, cancellationToken);
    }
}
