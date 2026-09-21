using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Application.Productos.UseCases;

public sealed class ObtenerCategoriasActivasUseCase
{
    private readonly ICategoriaRepository _categoriaRepository;

    public ObtenerCategoriasActivasUseCase(ICategoriaRepository categoriaRepository)
    {
        _categoriaRepository = categoriaRepository;
    }

    public async Task<IEnumerable<Categoria>> EjecutarAsync(CancellationToken cancellationToken = default)
    {
        return await _categoriaRepository.ObtenerActivasAsync(cancellationToken);
    }
}
