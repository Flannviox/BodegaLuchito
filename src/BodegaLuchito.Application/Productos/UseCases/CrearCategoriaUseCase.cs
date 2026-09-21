using System;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Application.Productos.UseCases;

public sealed class CrearCategoriaUseCase
{
    private readonly ICategoriaRepository _categoriaRepository;

    public CrearCategoriaUseCase(ICategoriaRepository categoriaRepository)
    {
        _categoriaRepository = categoriaRepository;
    }

    public async Task<Categoria> EjecutarAsync(CrearCategoriaRequest request, CancellationToken cancellationToken = default)
    {
        var nombreLimpio = request.Nombre?.Trim();

        if (string.IsNullOrWhiteSpace(nombreLimpio))
            throw new ArgumentException("El nombre de la categoría no puede estar vacío.");

        var existe = await _categoriaRepository.ExisteNombreAsync(nombreLimpio, cancellationToken);
        if (existe)
            throw new InvalidOperationException($"Ya existe una categoría activa con el nombre '{nombreLimpio}'.");

        var categoria = new Categoria
        {
            Nombre = nombreLimpio,
            Activo = true,
            FechaCreacion = DateTime.Now
        };

        await _categoriaRepository.AgregarAsync(categoria, cancellationToken);

        return categoria; // Retornamos la entidad para que la UI la seleccione automáticamente
    }
}
