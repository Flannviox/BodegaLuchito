using System.Text.RegularExpressions;
using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Domain.Proveedores.Entities;

namespace BodegaLuchito.Application.Proveedores.UseCases;

public class EditarProveedorUseCase
{
    private readonly IProveedorRepository _repository;

    public EditarProveedorUseCase(IProveedorRepository repository)
    {
        _repository = repository;
    }

    public async Task ExecuteAsync(int id, string? telefono, string? direccion)
    {
        var proveedor = await _repository.ObtenerPorIdAsync(id) 
            ?? throw new InvalidOperationException("Proveedor no encontrado.");

        var telefonoNorm = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        var direccionNorm = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();

        if (telefonoNorm != null && !Regex.IsMatch(telefonoNorm, @"^[0-9]{1,10}$"))
            throw new ArgumentException("El teléfono debe contener solo números (máximo 10 dígitos).");

        proveedor.Telefono = telefonoNorm;
        proveedor.Direccion = direccionNorm;
        proveedor.FechaActualizacion = DateTime.Now;

        await _repository.ActualizarAsync(proveedor);
    }
}
