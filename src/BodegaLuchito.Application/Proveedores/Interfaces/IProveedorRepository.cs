using BodegaLuchito.Domain.Proveedores.Entities;

namespace BodegaLuchito.Application.Proveedores.Interfaces;

public interface IProveedorRepository
{
    Task RegistrarAsync(Proveedor proveedor);
    Task<bool> ExisteRucAsync(string ruc);
    Task<List<Proveedor>> ListarActivosAsync();
}
