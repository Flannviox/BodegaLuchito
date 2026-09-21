using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Application.Productos.Interfaces;

public interface ICategoriaRepository
{
    Task AgregarAsync(Categoria categoria, CancellationToken cancellationToken = default);
    Task<bool> ExisteNombreAsync(string nombre, CancellationToken cancellationToken = default);
    Task<IEnumerable<Categoria>> ObtenerActivasAsync(CancellationToken cancellationToken = default);
}
