using BodegaLuchito.Application.Proveedores.DTOs;
using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Domain.Proveedores.Entities;

namespace BodegaLuchito.Application.Proveedores.UseCases;

public class RegistrarProveedorUseCase
{
    private readonly IProveedorRepository _repository;

    public RegistrarProveedorUseCase(IProveedorRepository repository)
    {
        _repository = repository;
    }

    public async Task<Proveedor> ExecuteAsync(RegistrarProveedorRequest request)
    {
        // Validaciones
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre del proveedor es requerido.");

        if (request.Nombre.Length > 150)
            throw new ArgumentException("El nombre no puede exceder los 150 caracteres.");

        if (!string.IsNullOrWhiteSpace(request.Ruc))
        {
            if (request.Ruc.Length != 11 || !request.Ruc.All(char.IsDigit))
                throw new ArgumentException("El RUC debe tener exactamente 11 dígitos.");

            if (await _repository.ExisteRucAsync(request.Ruc))
                throw new InvalidOperationException("El RUC ya se encuentra registrado.");
        }

        // Mapeo
        var proveedor = new Proveedor
        {
            Nombre = request.Nombre,
            Ruc = request.Ruc,
            Telefono = request.Telefono,
            Direccion = request.Direccion,
            Activo = true,
            FechaCreacion = DateTime.Now,
            FechaActualizacion = DateTime.Now
        };

        // Persistencia
        await _repository.RegistrarAsync(proveedor);

        return proveedor;
    }
}
