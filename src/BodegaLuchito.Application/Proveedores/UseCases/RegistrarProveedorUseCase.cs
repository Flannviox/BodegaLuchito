using System.Text.RegularExpressions;
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
        // 1. Normalización de campos
        var nombreNorm = request.Nombre?.Trim() ?? string.Empty;
        var rucNorm = string.IsNullOrWhiteSpace(request.Ruc) ? null : request.Ruc.Trim();
        var telefonoNorm = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
        var direccionNorm = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim();

        // 2. Validación de Nombre
        if (string.IsNullOrWhiteSpace(nombreNorm))
            throw new ArgumentException("El nombre del proveedor es requerido.");

        if (nombreNorm.Length > 150)
            throw new ArgumentException("El nombre no puede exceder los 150 caracteres.");

        // 3. Validación de RUC
        if (rucNorm != null)
        {
            if (!Regex.IsMatch(rucNorm, @"^[0-9]{11}$"))
                throw new ArgumentException("El RUC debe contener exactamente 11 dígitos numéricos.");

            if (await _repository.ExisteRucAsync(rucNorm))
                throw new InvalidOperationException("El RUC ya se encuentra registrado.");
        }

        // 4. Validación de Teléfono
        if (telefonoNorm != null && !Regex.IsMatch(telefonoNorm, @"^[0-9]{9}$"))
        {
            throw new ArgumentException("El teléfono debe contener exactamente 9 dígitos numéricos.");
        }

        // 5. Mapeo
        var proveedor = new Proveedor
        {
            Nombre = nombreNorm,
            Ruc = rucNorm,
            Telefono = telefonoNorm,
            Direccion = direccionNorm,
            Activo = true,
            FechaCreacion = DateTime.Now,
            FechaActualizacion = null // Se asignará solo al modificar
        };

        // 6. Persistencia
        await _repository.RegistrarAsync(proveedor);

        return proveedor;
    }
}
