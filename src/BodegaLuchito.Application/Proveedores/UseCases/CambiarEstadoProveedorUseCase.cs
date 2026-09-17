using BodegaLuchito.Application.Proveedores.Interfaces;

namespace BodegaLuchito.Application.Proveedores.UseCases;

public class CambiarEstadoProveedorUseCase
{
    private readonly IProveedorRepository _repository;

    public CambiarEstadoProveedorUseCase(IProveedorRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(int id, bool activar)
    {
        var proveedor = await _repository.ObtenerPorIdAsync(id)
            ?? throw new InvalidOperationException("Proveedor no encontrado.");

        proveedor.Activo = activar;
        proveedor.FechaActualizacion = DateTime.Now;

        await _repository.ActualizarAsync(proveedor);
    }
}
