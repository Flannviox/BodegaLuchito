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

    // Control de Modal y Vistas
    [ObservableProperty] private bool _isFormularioVisible;
    [ObservableProperty] private bool _isEdicion;
    [ObservableProperty] private string _tituloFormulario = "Nuevo proveedor";

    // Notificaciones (Snackbar)
    [ObservableProperty] private bool _isNotificacionVisible;
    [ObservableProperty] private string _mensajeNotificacion = string.Empty;

    // Búsqueda y Listas
    private List<Proveedor> _todosLosProveedores = new();
    public ObservableCollection<Proveedor> ListaProveedores { get; } = new();

    [ObservableProperty] private string _textoBusqueda = string.Empty;

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
        _todosLosProveedores = await _repository.ObtenerTodosAsync();
        FiltrarProveedores();
        ProveedorSeleccionado = null;
    }

    partial void OnTextoBusquedaChanged(string value)
    {
        FiltrarProveedores();
    }

    private void FiltrarProveedores()
    {
        var busqueda = TextoBusqueda?.ToLowerInvariant() ?? string.Empty;

        var filtrados = string.IsNullOrWhiteSpace(busqueda)
            ? _todosLosProveedores
            : _todosLosProveedores.Where(p =>
                (p.Nombre != null && p.Nombre.ToLowerInvariant().Contains(busqueda)) ||
                (p.Ruc != null && p.Ruc.Contains(busqueda))).ToList();

        ListaProveedores.Clear();
        foreach (var p in filtrados) ListaProveedores.Add(p);
    }

    // --- Lógica de Notificación Flotante ---
    private async Task MostrarNotificacionAsync(string mensaje)
    {
        MensajeNotificacion = mensaje;
        IsNotificacionVisible = true;
        await Task.Delay(3000); // Se oculta después de 3 segundos
        IsNotificacionVisible = false;
    }

    [RelayCommand]
    private void MostrarFormularioNuevo()
    {
        IsEdicion = false;
        TituloFormulario = "Nuevo proveedor";
        Nombre = string.Empty;
        Ruc = string.Empty;
        Telefono = string.Empty;
        Direccion = string.Empty;

        IsFormularioVisible = true;
    }

    private bool PuedeModificar() => ProveedorSeleccionado != null;

    [RelayCommand(CanExecute = nameof(PuedeModificar))]
    private void MostrarFormularioEditar()
    {
        if (ProveedorSeleccionado == null) return;

        IsEdicion = true;
        TituloFormulario = "Editar proveedor";
        Nombre = ProveedorSeleccionado.Nombre;
        Ruc = ProveedorSeleccionado.Ruc;
        Telefono = ProveedorSeleccionado.Telefono;
        Direccion = ProveedorSeleccionado.Direccion;

        IsFormularioVisible = true;
    }

    [RelayCommand]
    private void CancelarFormulario()
    {
        IsFormularioVisible = false;
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        try
        {
            if (IsEdicion)
            {
                await _editarUseCase.ExecuteAsync(ProveedorSeleccionado!.Id, Telefono, Direccion);
                _ = MostrarNotificacionAsync("✓ Proveedor actualizado correctamente.");
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
                _ = MostrarNotificacionAsync("✓ Proveedor registrado correctamente.");
            }

            IsFormularioVisible = false;
            await CargarProveedoresAsync();
        }
        catch (ArgumentException ex) { MessageBox.Show(ex.Message, "Validación", MessageBoxButton.OK, MessageBoxImage.Warning); }
        catch (InvalidOperationException ex) { MessageBox.Show(ex.Message, "Regla de Negocio", MessageBoxButton.OK, MessageBoxImage.Warning); }
        catch (Exception ex) { MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
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

            _ = MostrarNotificacionAsync(nuevoEstado
                ? "✓ Proveedor activado correctamente."
                : "✓ Proveedor desactivado correctamente.");
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
                _ = MostrarNotificacionAsync("✓ Proveedor eliminado correctamente.");
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
    }
}
