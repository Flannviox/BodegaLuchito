using BodegaLuchito.Domain.Productos.Entities;

namespace BodegaLuchito.Application.Productos.Interfaces;

public interface IProductoRepository
{
	Task AgregarAsync(Producto producto);
	Task<bool> ExisteCodigoBarrasAsync(string codigoBarras);
	Task<IEnumerable<Producto>> ObtenerActivosAsync();
}
