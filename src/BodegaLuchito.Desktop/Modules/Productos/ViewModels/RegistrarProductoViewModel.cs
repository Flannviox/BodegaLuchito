using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Productos.ViewModels;

public partial class RegistrarProductoViewModel : ViewModelBase
{
    private readonly RegistrarProductoUseCase _registrarUseCase;
    private readonly ModificarProductoUseCase _modificarUseCase;
    private readonly EliminarProductoUseCase _eliminarUseCase;
    private readonly IProductoRepository _productoRepository;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _categoria = string.Empty;
    [ObservableProperty] private string? _codigoBarras;
    [ObservableProperty] private decimal _precioVenta;
    [ObservableProperty] private UnidadVenta _unidadVenta = UnidadVenta.Unidad;
    [ObservableProperty] private bool _controlaInventario = true;
    [ObservableProperty] private decimal _stockActual;
    [ObservableProperty] private decimal _stockMinimo;

    [ObservableProperty] private string _textoBusqueda = string.Empty;
    private List<Producto> _productosOriginales = new();

    [ObservableProperty] private Visibility _modalVisible = Visibility.Collapsed;
    [ObservableProperty] private string _tituloModal = "Nuevo Producto";

    // Nueva propiedad para la UI
    [ObservableProperty] private string _sufijoUnidad = "(unid)";

    private int _idProductoEdicion = 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditarCommand))]
    [NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
    private Producto? _productoSeleccionado;

    public IEnumerable<UnidadVenta> UnidadesVenta => Enum.GetValues(typeof(UnidadVenta)).Cast<UnidadVenta>();
    public ObservableCollection<Producto> Productos { get; } = new();

    public RegistrarProductoViewModel(
        RegistrarProductoUseCase registrarUseCase,
        ModificarProductoUseCase modificarUseCase,
        EliminarProductoUseCase eliminarUseCase,
        IProductoRepository productoRepository)
    {
        _registrarUseCase = registrarUseCase;
        _modificarUseCase = modificarUseCase;
        _eliminarUseCase = eliminarUseCase;
        _productoRepository = productoRepository;

        CargarProductosAsync().ConfigureAwait(false);
    }

    // Este método mágico se ejecuta automáticamente cuando cambia la UnidadVenta
    partial void OnUnidadVentaChanged(UnidadVenta value)
    {
        SufijoUnidad = value == UnidadVenta.Peso ? "(kg)" : "(unid)";
    }

    partial void OnTextoBusquedaChanged(string value)
    {
        AplicarFiltro();
    }

    private void AplicarFiltro()
    {
        var busqueda = TextoBusqueda?.ToLower().Trim() ?? string.Empty;
        var filtrados = string.IsNullOrWhiteSpace(busqueda)
            ? _productosOriginales
            : _productosOriginales.Where(p =>
                p.Nombre.ToLower().Contains(busqueda) ||
                p.Categoria.ToLower().Contains(busqueda) ||
                (p.CodigoBarras != null && p.CodigoBarras.ToLower().Contains(busqueda))
            ).ToList();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            Productos.Clear();
            foreach (var p in filtrados)
            {
                Productos.Add(p);
            }
        });
    }

    private bool PuedeEditarOEliminar() => ProductoSeleccionado != null;

    [RelayCommand]
    private void AbrirModalNuevo()
    {
        _idProductoEdicion = 0;
        TituloModal = "Nuevo Producto";
        LimpiarFormulario();
        ModalVisible = Visibility.Visible;
    }

    [RelayCommand(CanExecute = nameof(PuedeEditarOEliminar))]
    private void Editar()
    {
        if (ProductoSeleccionado == null) return;

        _idProductoEdicion = ProductoSeleccionado.Id;
        TituloModal = "Editar Producto";

        Nombre = ProductoSeleccionado.Nombre;
        Categoria = ProductoSeleccionado.Categoria;
        CodigoBarras = ProductoSeleccionado.CodigoBarras;
        PrecioVenta = ProductoSeleccionado.PrecioVenta;
        UnidadVenta = ProductoSeleccionado.UnidadVenta; // Esto disparará el cambio de Sufijo
        ControlaInventario = ProductoSeleccionado.ControlaInventario;
        StockActual = ProductoSeleccionado.StockActual;
        StockMinimo = ProductoSeleccionado.StockMinimo;

        ModalVisible = Visibility.Visible;
    }

    [RelayCommand(CanExecute = nameof(PuedeEditarOEliminar))]
    private async Task EliminarAsync()
    {
        if (ProductoSeleccionado == null) return;

        var result = MessageBox.Show(
            $"¿Está seguro que desea eliminar el producto '{ProductoSeleccionado.Nombre}'?\nEsta acción lo ocultará del inventario.",
            "Bodega Luchito - Confirmar Eliminación",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                await _eliminarUseCase.EjecutarAsync(ProductoSeleccionado.Id);
                await CargarProductosAsync();
                MessageBox.Show("Producto eliminado con éxito.", "Bodega Luchito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        try
        {
            if (_idProductoEdicion == 0)
            {
                var request = new RegistrarProductoRequest(Nombre, Categoria, CodigoBarras, PrecioVenta, UnidadVenta, ControlaInventario, StockActual, StockMinimo);
                await _registrarUseCase.EjecutarAsync(request);
                MessageBox.Show("Producto registrado con éxito.", "Bodega Luchito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var request = new ModificarProductoRequest(_idProductoEdicion, Nombre, Categoria, CodigoBarras, PrecioVenta, UnidadVenta, ControlaInventario, StockActual, StockMinimo);
                await _modificarUseCase.EjecutarAsync(request);
                MessageBox.Show("Producto actualizado con éxito.", "Bodega Luchito", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            await CargarProductosAsync();
            CerrarModal();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void CerrarModal()
    {
        LimpiarFormulario();
        ModalVisible = Visibility.Collapsed;
    }

    private async Task CargarProductosAsync()
    {
        var productosBD = await _productoRepository.ObtenerActivosAsync();
        _productosOriginales = productosBD.OrderByDescending(p => p.Id).ToList();
        AplicarFiltro();
    }

    private void LimpiarFormulario()
    {
        Nombre = string.Empty;
        Categoria = string.Empty;
        CodigoBarras = null;
        PrecioVenta = 0;
        UnidadVenta = UnidadVenta.Unidad; // Vuelve al default y cambia el sufijo
        ControlaInventario = true;
        StockActual = 0;
        StockMinimo = 0;
        ProductoSeleccionado = null;
    }
}
