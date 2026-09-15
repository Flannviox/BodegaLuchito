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
    private readonly EditarProveedorUseCase _editarUseCase;
    private readonly CambiarEstadoProveedorUseCase _cambiarEstadoUseCase;
    private readonly EliminarProveedorUseCase _eliminarUseCase;
    private readonly IProveedorRepository _repository;

    // Control de Vistas
    [ObservableProperty] private bool _isListaVisible = true;
    [ObservableProperty] private bool _isFormularioVisible = false;
    [ObservableProperty] private bool _isEdicion;
    [ObservableProperty] private string _tituloFormulario = "Nuevo Proveedor";

    // Selección de Tabla
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(MostrarFormularioEditarCommand))]
    [NotifyCanExecuteChangedFor(nameof(CambiarEstadoCommand))]
    [NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
    [NotifyPropertyChangedFor(nameof(TextoBotonEstado))]
    private Proveedor? _proveedorSeleccionado;

    // Campos del Formulario
    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string? _ruc;
    [ObservableProperty] private string? _telefono;
    [ObservableProperty] private string? _direccion;

    public ObservableCollection<Proveedor> ListaProveedores { get; } = new();

    public string TextoBotonEstado => ProveedorSeleccionado?.Activo == true ? "Inhabilitar" : "Activar";

    public ProveedoresViewModel(
        RegistrarProveedorUseCase registrarUseCase,
        EditarProveedorUseCase editarUseCase,
        CambiarEstadoProveedorUseCase cambiarEstadoUseCase,
        EliminarProveedorUseCase eliminarUseCase,
        IProveedorRepository repository)
    {
        _registrarUseCase = registrarUseCase;
        _editarUseCase = editarUseCase;
        _cambiarEstadoUseCase = cambiarEstadoUseCase;
        _eliminarUseCase = eliminarUseCase;
        _repository = repository;

        _ = CargarInicialAsync();
    }

    private async Task CargarInicialAsync()
    {
        try { await CargarProveedoresAsync(); }
        catch (Exception ex) { MessageBox.Show($"Error crítico: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private async Task CargarProveedoresAsync()
    {
        var proveedores = await _repository.ObtenerTodosAsync(); // <- ¡Aquí solucionamos tu error!
        ListaProveedores.Clear();
        foreach (var p in proveedores) ListaProveedores.Add(p);
        ProveedorSeleccionado = null; // Reiniciar selección
    }

    [RelayCommand]
    private void MostrarFormularioNuevo()
    {
        IsEdicion = false;
        TituloFormulario = "Nuevo Proveedor";
        Nombre = string.Empty;
        Ruc = string.Empty;
        Telefono = string.Empty;
        Direccion = string.Empty;

        IsListaVisible = false;
        IsFormularioVisible = true;
    }

    private bool PuedeModificar() => ProveedorSeleccionado != null;

    [RelayCommand(CanExecute = nameof(PuedeModificar))]
    private void MostrarFormularioEditar()
    {
        if (ProveedorSeleccionado == null) return;

        IsEdicion = true;
        TituloFormulario = "Editar Proveedor";
        Nombre = ProveedorSeleccionado.Nombre;
        Ruc = ProveedorSeleccionado.Ruc;
        Telefono = ProveedorSeleccionado.Telefono;
        Direccion = ProveedorSeleccionado.Direccion;

        IsListaVisible = false;
        IsFormularioVisible = true;
    }

    [RelayCommand]
    private void CancelarFormulario()
    {
        IsFormularioVisible = false;
        IsListaVisible = true;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        try
        {
            if (IsEdicion)
            {
                await _editarUseCase.ExecuteAsync(ProveedorSeleccionado!.Id, Telefono, Direccion);
                MessageBox.Show("Proveedor actualizado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
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
            }

            IsFormularioVisible = false;
            IsListaVisible = true;
            await CargarProveedoresAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeModificar))]
    private async Task CambiarEstadoAsync()
    {
        if (ProveedorSeleccionado == null) return;
        try
        {
            bool nuevoEstado = !ProveedorSeleccionado.Activo;
            await _cambiarEstadoUseCase.ExecuteAsync(ProveedorSeleccionado.Id, nuevoEstado);
            await CargarProveedoresAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    [RelayCommand(CanExecute = nameof(PuedeModificar))]
    private async Task EliminarAsync()
    {
        if (ProveedorSeleccionado == null) return;

        var r = MessageBox.Show($"¿Estás seguro de eliminar a '{ProveedorSeleccionado.Nombre}' permanentemente?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
        {
            try
            {
                await _eliminarUseCase.ExecuteAsync(ProveedorSeleccionado.Id);
                await CargarProveedoresAsync();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
