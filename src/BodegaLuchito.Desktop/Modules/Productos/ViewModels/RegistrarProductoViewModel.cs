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

    // Inyección de nuevos Use Cases para Categoría
    private readonly ObtenerCategoriasActivasUseCase _obtenerCategoriasUseCase;
    private readonly CrearCategoriaUseCase _crearCategoriaUseCase;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private Categoria? _categoriaSeleccionada;
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
    [ObservableProperty] private string _sufijoUnidad = "(unid)";

    // Propiedades para el Modal de Nueva Categoría
    [ObservableProperty] private Visibility _modalCategoriaVisible = Visibility.Collapsed;
    [ObservableProperty] private string _nuevaCategoriaNombre = string.Empty;

    private int _idProductoEdicion = 0;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EditarCommand))]
    [NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
    private Producto? _productoSeleccionado;

    public IEnumerable<UnidadVenta> UnidadesVenta => Enum.GetValues(typeof(UnidadVenta)).Cast<UnidadVenta>();
    public ObservableCollection<Producto> Productos { get; } = new();
    public ObservableCollection<Categoria> CategoriasDisponibles { get; } = new();

    public RegistrarProductoViewModel(
        RegistrarProductoUseCase registrarUseCase,
        ModificarProductoUseCase modificarUseCase,
        EliminarProductoUseCase eliminarUseCase,
        IProductoRepository productoRepository,
        ObtenerCategoriasActivasUseCase obtenerCategoriasUseCase,
        CrearCategoriaUseCase crearCategoriaUseCase)
    {
        _registrarUseCase = registrarUseCase;
        _modificarUseCase = modificarUseCase;
        _eliminarUseCase = eliminarUseCase;
        _productoRepository = productoRepository;
        _obtenerCategoriasUseCase = obtenerCategoriasUseCase;
        _crearCategoriaUseCase = crearCategoriaUseCase;

        CargarDatosInicialesAsync().ConfigureAwait(false);
    }

    private async Task CargarDatosInicialesAsync()
    {
        await CargarCategoriasAsync();
        await CargarProductosAsync();
    }

    private async Task CargarCategoriasAsync()
    {
        var categorias = await _obtenerCategoriasUseCase.EjecutarAsync();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CategoriasDisponibles.Clear();
            foreach (var cat in categorias)
            {
                CategoriasDisponibles.Add(cat);
            }
        });
    }

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
                (p.Categoria != null && p.Categoria.Nombre.ToLower().Contains(busqueda)) ||
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
        CategoriaSeleccionada = CategoriasDisponibles.FirstOrDefault(c => c.Id == ProductoSeleccionado.CategoriaId);
        CodigoBarras = ProductoSeleccionado.CodigoBarras;
        PrecioVenta = ProductoSeleccionado.PrecioVenta;
        UnidadVenta = ProductoSeleccionado.UnidadVenta;
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
            if (CategoriaSeleccionada == null)
            {
                MessageBox.Show("Debe seleccionar una categoría.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_idProductoEdicion == 0)
            {
                var request = new RegistrarProductoRequest(Nombre, CategoriaSeleccionada.Id, CodigoBarras, PrecioVenta, UnidadVenta, ControlaInventario, StockActual, StockMinimo);
                await _registrarUseCase.EjecutarAsync(request);
                MessageBox.Show("Producto registrado con éxito.", "Bodega Luchito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var request = new ModificarProductoRequest(_idProductoEdicion, Nombre, CategoriaSeleccionada.Id, CodigoBarras, PrecioVenta, UnidadVenta, ControlaInventario, StockActual, StockMinimo);
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

    // Comandos del Modal de Categoría
    [RelayCommand]
    private void AbrirModalNuevaCategoria()
    {
        NuevaCategoriaNombre = string.Empty;
        ModalCategoriaVisible = Visibility.Visible;
    }

    [RelayCommand]
    private void CerrarModalCategoria()
    {
        NuevaCategoriaNombre = string.Empty;
        ModalCategoriaVisible = Visibility.Collapsed;
    }

    [RelayCommand]
    private async Task GuardarNuevaCategoriaAsync()
    {
        try
        {
            var request = new CrearCategoriaRequest(NuevaCategoriaNombre);
            var nuevaCategoria = await _crearCategoriaUseCase.EjecutarAsync(request);

            await CargarCategoriasAsync();

            // Auto-seleccionamos la categoría recién creada para evitar que la usuaria tenga que buscarla
            CategoriaSeleccionada = CategoriasDisponibles.FirstOrDefault(c => c.Id == nuevaCategoria.Id);

            MessageBox.Show("Categoría creada con éxito.", "Bodega Luchito", MessageBoxButton.OK, MessageBoxImage.Information);
            CerrarModalCategoria();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
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
        CategoriaSeleccionada = null;
        CodigoBarras = null;
        PrecioVenta = 0;
        UnidadVenta = UnidadVenta.Unidad;
        ControlaInventario = true;
        StockActual = 0;
        StockMinimo = 0;
        ProductoSeleccionado = null;
    }
}
