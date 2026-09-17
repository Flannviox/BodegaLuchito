using BodegaLuchito.Application.Proveedores.Interfaces;

namespace BodegaLuchito.Application.Proveedores.UseCases;

public class EliminarProveedorUseCase
{
    private readonly IProveedorRepository _repository;

    public EliminarProveedorUseCase(IProveedorRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(int id)
    {
        var proveedor = await _repository.ObtenerPorIdAsync(id)
            ?? throw new InvalidOperationException("Proveedor no encontrado.");

        await _repository.EliminarAsync(proveedor);
    }
}
