using BodegaLuchito.Domain.Proveedores.Entities;

namespace BodegaLuchito.Application.Proveedores.Interfaces;

public interface IProveedorRepository
{
    Task RegistrarAsync(Proveedor proveedor);
    Task<bool> ExisteRucAsync(string ruc);
    Task<List<Proveedor>> ObtenerTodosAsync(); // Para ver activos e inactivos en la tabla
    Task<Proveedor?> ObtenerPorIdAsync(int id);
    Task ActualizarAsync(Proveedor proveedor);
    Task EliminarAsync(Proveedor proveedor);
}
