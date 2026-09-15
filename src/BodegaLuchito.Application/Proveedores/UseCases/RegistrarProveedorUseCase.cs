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
        var nombreNorm = request.Nombre?.Trim() ?? string.Empty;
        var rucNorm = request.Ruc?.Trim() ?? string.Empty;
        var telefonoNorm = string.IsNullOrWhiteSpace(request.Telefono) ? null : request.Telefono.Trim();
        var direccionNorm = string.IsNullOrWhiteSpace(request.Direccion) ? null : request.Direccion.Trim();

        if (string.IsNullOrWhiteSpace(nombreNorm) || nombreNorm.Length > 150)
            throw new ArgumentException("El nombre es requerido y no puede exceder los 150 caracteres.");

        // RUC ahora es OBLIGATORIO
        if (!Regex.IsMatch(rucNorm, @"^[0-9]{11}$"))
            throw new ArgumentException("El RUC es obligatorio y debe contener exactamente 11 dígitos numéricos.");

        if (await _repository.ExisteRucAsync(rucNorm))
            throw new InvalidOperationException("El RUC ya se encuentra registrado.");

        // Teléfono ahora es MÁXIMO 11 DÍGITOS numéricos
        if (telefonoNorm != null && !Regex.IsMatch(telefonoNorm, @"^[0-9]{1,10}$"))
            throw new ArgumentException("El teléfono debe contener solo números (máximo 10 dígitos).");

        var proveedor = new Proveedor
        {
            Nombre = nombreNorm,
            Ruc = rucNorm,
            Telefono = telefonoNorm,
            Direccion = direccionNorm,
            Activo = true,
            FechaCreacion = DateTime.Now,
            FechaActualizacion = null
        };

        await _repository.RegistrarAsync(proveedor);
        return proveedor;
    }
}
