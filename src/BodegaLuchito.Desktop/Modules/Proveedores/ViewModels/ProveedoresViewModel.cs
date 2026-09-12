using System.Collections.ObjectModel;
using System.Windows;
using BodegaLuchito.Application.Proveedores.DTOs;
using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Application.Proveedores.UseCases;
using BodegaLuchito.Domain.Proveedores.Entities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Proveedores.ViewModels;

public partial class ProveedoresViewModel : ObservableObject
{
    private readonly RegistrarProveedorUseCase _registrarUseCase;
    private readonly IProveedorRepository _repository;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _ruc;
    [ObservableProperty] private string? _telefono;
    [ObservableProperty] private string? _direccion;

    public ObservableCollection<Proveedor> ListaProveedores { get; } = new();

    public ProveedoresViewModel(RegistrarProveedorUseCase registrarUseCase, IProveedorRepository repository)
    {
        _registrarUseCase = registrarUseCase;
        _repository = repository;
        _ = CargarProveedoresAsync();
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        try
        {
            var request = new RegistrarProveedorRequest
            {
                Nombre = Nombre,
                Ruc = string.IsNullOrWhiteSpace(Ruc) ? null : Ruc,
                Telefono = string.IsNullOrWhiteSpace(Telefono) ? null : Telefono,
                Direccion = string.IsNullOrWhiteSpace(Direccion) ? null : Direccion
            };

            await _registrarUseCase.ExecuteAsync(request);

            MessageBox.Show("Proveedor registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            LimpiarFormulario();
            await CargarProveedoresAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task CargarProveedoresAsync()
    {
        var proveedores = await _repository.ListarActivosAsync();
        ListaProveedores.Clear();
        foreach (var p in proveedores)
        {
            ListaProveedores.Add(p);
        }
    }

    private void LimpiarFormulario()
    {
        Nombre = string.Empty;
        Ruc = null;
        Telefono = null;
        Direccion = null;
    }
}
