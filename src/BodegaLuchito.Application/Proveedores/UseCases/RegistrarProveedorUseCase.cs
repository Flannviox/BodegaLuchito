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
        // Validar Nombre: Requerido, máximo 150 caracteres y SOLO letras/espacios
        if (string.IsNullOrWhiteSpace(request.Nombre))
            throw new ArgumentException("El nombre del proveedor es requerido.");

        if (request.Nombre.Length > 150)
            throw new ArgumentException("El nombre no puede exceder los 150 caracteres.");

        if (!Regex.IsMatch(request.Nombre, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$"))
            throw new ArgumentException("El nombre solo puede contener letras y espacios (no números).");

        // Validar RUC: Opcional, pero si existe deben ser EXACTAMENTE 11 números
        if (!string.IsNullOrWhiteSpace(request.Ruc))
        {
            if (!Regex.IsMatch(request.Ruc, @"^\d{11}$"))
                throw new ArgumentException("El RUC debe contener exactamente 11 dígitos numéricos.");

            if (await _repository.ExisteRucAsync(request.Ruc))
                throw new InvalidOperationException("El RUC ya se encuentra registrado.");
        }

        // Validar Teléfono: Opcional, pero si existe deben ser EXACTAMENTE 9 números
        if (!string.IsNullOrWhiteSpace(request.Telefono))
        {
            if (!Regex.IsMatch(request.Telefono, @"^\d{9}$"))
                throw new ArgumentException("El teléfono debe contener exactamente 9 dígitos numéricos.");
        }

        // Validar Dirección: Letras, números y caracteres comunes
        if (!string.IsNullOrWhiteSpace(request.Direccion))
        {
            if (!Regex.IsMatch(request.Direccion, @"^[a-zA-Z0-9áéíóúÁÉÍÓÚñÑ\s\.,#-]+$"))
                throw new ArgumentException("La dirección contiene caracteres no permitidos.");
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
