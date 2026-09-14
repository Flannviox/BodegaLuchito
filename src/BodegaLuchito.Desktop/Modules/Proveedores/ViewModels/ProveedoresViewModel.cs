using System.Collections.ObjectModel;
using System.Windows;
using BodegaLuchito.Application.Proveedores.DTOs;
using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Application.Proveedores.UseCases;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Proveedores.ViewModels;

public partial class ProveedoresViewModel : ViewModelBase
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
        _ = CargarInicialAsync(); // Fire and forget controlado
    }

    private async Task CargarInicialAsync()
    {
        try
        {
            await CargarProveedoresAsync();
        }
        catch (Exception ex)
        {
            // Fallos de base de datos o inicialización
            MessageBox.Show($"Error crítico al cargar datos: {ex.Message}", "Error de Persistencia", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        try
        {
            var request = new RegistrarProveedorRequest
            {
                Nombre = Nombre,
                Ruc = Ruc,
                Telefono = Telefono,
                Direccion = Direccion
            };

            await _registrarUseCase.ExecuteAsync(request);

            MessageBox.Show("Proveedor registrado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            LimpiarFormulario();
            await CargarProveedoresAsync();
        }
        catch (ArgumentException ex) // Errores de Validación (Inputs)
        {
            MessageBox.Show(ex.Message, "Validación de Campos", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (InvalidOperationException ex) // Errores de Negocio (Duplicados)
        {
            MessageBox.Show(ex.Message, "Regla de Negocio", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex) // Errores no controlados o base de datos
        {
            MessageBox.Show($"Ocurrió un error al guardar: {ex.Message}", "Error de Persistencia", MessageBoxButton.OK, MessageBoxImage.Error);
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
