using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Desktop.Common.ViewModels;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Productos.ViewModels;

public partial class RegistrarProductoViewModel : ViewModelBase
{
    private readonly RegistrarProductoUseCase _registrarUseCase;
    private readonly ModificarProductoUseCase _modificarUseCase;
    private readonly EliminarProductoUseCase _eliminarUseCase;
    private readonly ReactivarProductoUseCase _reactivarUseCase;
    private readonly ConsultarHistorialProductoUseCase _consultarHistorialProductoUseCase;
    private readonly ConsultarHistorialGeneralProductosUseCase _consultarHistorialGeneralProductosUseCase;
    private readonly IProductoRepository _productoRepository;
    private readonly ObtenerCategoriasActivasUseCase _obtenerCategoriasUseCase;
    private readonly CrearCategoriaUseCase _crearCategoriaUseCase;

    private List<Producto> _productosOriginales = [];
    private int _idProductoEdicion;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private Categoria? _categoriaSeleccionada;
    [ObservableProperty] private string? _codigoBarras;
    [ObservableProperty] private decimal _precioVenta;
    [ObservableProperty] private UnidadVenta _unidadVenta = UnidadVenta.Unidad;
    [ObservableProperty] private decimal _stockActual;
    [ObservableProperty] private decimal _stockMinimo;
    [ObservableProperty] private bool _formularioHabilitado = true;
    [ObservableProperty] private string _tituloFormulario = "Nuevo producto";
    [ObservableProperty] private string _sufijoUnidad = "(unid)";

    [ObservableProperty] private string _textoBusqueda = string.Empty;
    [ObservableProperty] private string _categoriaFiltroSeleccionada = "Todas";
    [ObservableProperty] private string _tipoVentaFiltroSeleccionado = "Todos";
    [ObservableProperty] private string _estadoFiltroSeleccionado = "Todos";

    [ObservableProperty] private Visibility _modalHistorialPreciosVisible = Visibility.Collapsed;
    [ObservableProperty] private Visibility _modalHistorialGeneralVisible = Visibility.Collapsed;
    [ObservableProperty] private string _nombreProductoHistorial = string.Empty;
    [ObservableProperty] private Visibility _modalConfirmarDesactivacionVisible = Visibility.Collapsed;
    [ObservableProperty] private Visibility _modalConfirmarReactivacionVisible = Visibility.Collapsed;
    [ObservableProperty] private Visibility _modalCategoriaVisible = Visibility.Collapsed;
    [ObservableProperty] private string _nuevaCategoriaNombre = string.Empty;

    [ObservableProperty] private bool _isNotificacionVisible;
    [ObservableProperty] private string _mensajeNotificacion = string.Empty;

    [ObservableProperty] private int _totalProductos;
    [ObservableProperty] private int _totalProductosActivos;
    [ObservableProperty] private int _totalProductosInactivos;
    [ObservableProperty] private int _productosConStockBajo;
    [ObservableProperty] private int _productosMostrados;
    [ObservableProperty] private Visibility _accionesProductoActivoVisible = Visibility.Collapsed;
    [ObservableProperty] private Visibility _accionesProductoInactivoVisible = Visibility.Collapsed;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EliminarCommand))]
    [NotifyCanExecuteChangedFor(nameof(ReactivarCommand))]
    [NotifyCanExecuteChangedFor(nameof(VerHistorialProductoCommand))]
    private Producto? _productoSeleccionado;

    public IEnumerable<UnidadVenta> UnidadesVenta => Enum.GetValues<UnidadVenta>();
    public IReadOnlyList<string> TiposVentaFiltro { get; } = ["Todos", "Unidad", "Peso"];
    public IReadOnlyList<string> EstadosFiltro { get; } = ["Todos", "Activos", "Desactivados"];
    public ObservableCollection<Producto> Productos { get; } = [];
    public ObservableCollection<Categoria> CategoriasDisponibles { get; } = [];
    public ObservableCollection<string> CategoriasFiltro { get; } = ["Todas"];
    public ObservableCollection<HistorialProductoItem> HistorialProducto { get; } = [];
    public ObservableCollection<HistorialProductoGeneralItem> HistorialGeneralProductos { get; } = [];
    public string TextoAccionFormulario => _idProductoEdicion == 0 ? "Guardar" : "Actualizar";

    public RegistrarProductoViewModel(
        RegistrarProductoUseCase registrarUseCase,
        ModificarProductoUseCase modificarUseCase,
        EliminarProductoUseCase eliminarUseCase,
        ReactivarProductoUseCase reactivarUseCase,
        ConsultarHistorialProductoUseCase consultarHistorialProductoUseCase,
        ConsultarHistorialGeneralProductosUseCase consultarHistorialGeneralProductosUseCase,
        IProductoRepository productoRepository,
        ObtenerCategoriasActivasUseCase obtenerCategoriasUseCase,
        CrearCategoriaUseCase crearCategoriaUseCase)
    {
        _registrarUseCase = registrarUseCase;
        _modificarUseCase = modificarUseCase;
        _eliminarUseCase = eliminarUseCase;
        _reactivarUseCase = reactivarUseCase;
        _consultarHistorialProductoUseCase = consultarHistorialProductoUseCase;
        _consultarHistorialGeneralProductosUseCase = consultarHistorialGeneralProductosUseCase;
        _productoRepository = productoRepository;
        _obtenerCategoriasUseCase = obtenerCategoriasUseCase;
        _crearCategoriaUseCase = crearCategoriaUseCase;

        _ = CargarDatosInicialesAsync();
    }

    private async Task CargarDatosInicialesAsync()
    {
        await CargarCategoriasAsync();
        await CargarProductosAsync();
        PrepararNuevoProducto();
    }

    private async Task CargarCategoriasAsync()
    {
        var categorias = await _obtenerCategoriasUseCase.EjecutarAsync();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            CategoriasDisponibles.Clear();
            CategoriasFiltro.Clear();
            CategoriasFiltro.Add("Todas");

            foreach (var categoria in categorias)
            {
                CategoriasDisponibles.Add(categoria);
                CategoriasFiltro.Add(categoria.Nombre);
            }

            if (!CategoriasFiltro.Contains(CategoriaFiltroSeleccionada))
                CategoriaFiltroSeleccionada = "Todas";
        });
    }

    partial void OnUnidadVentaChanged(UnidadVenta value)
    {
        SufijoUnidad = value == UnidadVenta.Peso ? "(kg)" : "(unid)";
    }

    partial void OnTextoBusquedaChanged(string value) => AplicarFiltros();
    partial void OnCategoriaFiltroSeleccionadaChanged(string value) => AplicarFiltros();
    partial void OnTipoVentaFiltroSeleccionadoChanged(string value) => AplicarFiltros();
    partial void OnEstadoFiltroSeleccionadoChanged(string value) => AplicarFiltros();

    partial void OnProductoSeleccionadoChanged(Producto? value)
    {
        AccionesProductoActivoVisible = value?.Activo == true
            ? Visibility.Visible
            : Visibility.Collapsed;
        AccionesProductoInactivoVisible = value is { Activo: false }
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (value is not null)
            CargarFormularioProducto(value);
    }

    private void AplicarFiltros()
    {
        IEnumerable<Producto> filtrados = _productosOriginales;
        var busqueda = TextoBusqueda?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            filtrados = filtrados.Where(producto =>
                producto.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ||
                (producto.Categoria?.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (producto.CodigoBarras?.Contains(busqueda, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        if (CategoriaFiltroSeleccionada != "Todas")
            filtrados = filtrados.Where(x => x.Categoria?.Nombre == CategoriaFiltroSeleccionada);

        if (TipoVentaFiltroSeleccionado != "Todos")
            filtrados = filtrados.Where(x => x.UnidadVenta.ToString() == TipoVentaFiltroSeleccionado);

        filtrados = EstadoFiltroSeleccionado switch
        {
            "Activos" => filtrados.Where(x => x.Activo),
            "Desactivados" => filtrados.Where(x => !x.Activo),
            _ => filtrados
        };

        var resultado = filtrados.OrderByDescending(x => x.Id).ToList();

        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            Productos.Clear();
            foreach (var producto in resultado)
                Productos.Add(producto);

            ProductosMostrados = resultado.Count;

            if (ProductoSeleccionado is not null && !resultado.Any(x => x.Id == ProductoSeleccionado.Id))
                ProductoSeleccionado = null;
        });
    }

    private bool PuedeDesactivar() => ProductoSeleccionado?.Activo == true;
    private bool PuedeReactivar() => ProductoSeleccionado?.Activo == false;
    private bool PuedeVerHistorial() => ProductoSeleccionado is not null;

    private async Task MostrarNotificacionAsync(string mensaje)
    {
        MensajeNotificacion = mensaje;
        IsNotificacionVisible = true;
        await Task.Delay(3000);
        IsNotificacionVisible = false;
    }

    [RelayCommand]
    private void AbrirModalNuevo() => PrepararNuevoProducto();

    [RelayCommand]
    private void Buscar() => AplicarFiltros();

    [RelayCommand]
    private void LimpiarFiltros()
    {
        TextoBusqueda = string.Empty;
        CategoriaFiltroSeleccionada = "Todas";
        TipoVentaFiltroSeleccionado = "Todos";
        EstadoFiltroSeleccionado = "Todos";
        AplicarFiltros();
    }

    [RelayCommand(CanExecute = nameof(PuedeDesactivar))]
    private void Eliminar()
    {
        if (ProductoSeleccionado is not null)
            ModalConfirmarDesactivacionVisible = Visibility.Visible;
    }

    [RelayCommand]
    private void CancelarDesactivacion() => ModalConfirmarDesactivacionVisible = Visibility.Collapsed;

    [RelayCommand]
    private async Task ConfirmarDesactivacionAsync()
    {
        if (ProductoSeleccionado is null)
            return;

        try
        {
            await _eliminarUseCase.EjecutarAsync(ProductoSeleccionado.Id);
            ModalConfirmarDesactivacionVisible = Visibility.Collapsed;
            await CargarProductosAsync();
            PrepararNuevoProducto();
            _ = MostrarNotificacionAsync("✓ Producto desactivado correctamente.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Producto", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeReactivar))]
    private void Reactivar()
    {
        if (ProductoSeleccionado is not null)
            ModalConfirmarReactivacionVisible = Visibility.Visible;
    }

    [RelayCommand]
    private void CancelarReactivacion() => ModalConfirmarReactivacionVisible = Visibility.Collapsed;

    [RelayCommand]
    private async Task ConfirmarReactivacionAsync()
    {
        if (ProductoSeleccionado is null)
            return;

        try
        {
            await _reactivarUseCase.EjecutarAsync(ProductoSeleccionado.Id);
            ModalConfirmarReactivacionVisible = Visibility.Collapsed;
            await CargarProductosAsync();
            PrepararNuevoProducto();
            _ = MostrarNotificacionAsync("✓ Producto reactivado correctamente.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Producto", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(PuedeVerHistorial))]
    private async Task VerHistorialProductoAsync()
    {
        if (ProductoSeleccionado is null)
            return;

        try
        {
            var historial = await _consultarHistorialProductoUseCase.EjecutarAsync(ProductoSeleccionado.Id);
            HistorialProducto.Clear();
            foreach (var cambio in historial)
                HistorialProducto.Add(cambio);

            NombreProductoHistorial = ProductoSeleccionado.Nombre;
            ModalHistorialPreciosVisible = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Historial del producto", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void CerrarModalHistorialPrecios() => ModalHistorialPreciosVisible = Visibility.Collapsed;

    [RelayCommand]
    private async Task VerHistorialGeneralAsync()
    {
        try
        {
            var historial = await _consultarHistorialGeneralProductosUseCase.EjecutarAsync();
            HistorialGeneralProductos.Clear();
            foreach (var cambio in historial)
                HistorialGeneralProductos.Add(cambio);

            ModalHistorialGeneralVisible = Visibility.Visible;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Historial general", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void CerrarModalHistorialGeneral() => ModalHistorialGeneralVisible = Visibility.Collapsed;

    [RelayCommand]
    private async Task GuardarAsync()
    {
        try
        {
            if (CategoriaSeleccionada is null)
            {
                MessageBox.Show("Debe seleccionar una categoría.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_idProductoEdicion == 0)
            {
                var request = new RegistrarProductoRequest(
                    Nombre, CategoriaSeleccionada.Id, CodigoBarras, PrecioVenta, UnidadVenta,
                    true, StockActual, StockMinimo);
                await _registrarUseCase.EjecutarAsync(request);
                _ = MostrarNotificacionAsync("✓ Producto registrado correctamente.");
            }
            else
            {
                var request = new ModificarProductoRequest(
                    _idProductoEdicion, Nombre, CategoriaSeleccionada.Id, CodigoBarras, PrecioVenta,
                    UnidadVenta, true, StockActual, StockMinimo);
                await _modificarUseCase.EjecutarAsync(request);
                _ = MostrarNotificacionAsync("✓ Producto actualizado correctamente.");
            }

            await CargarProductosAsync();
            PrepararNuevoProducto();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

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
            var categoria = await _crearCategoriaUseCase.EjecutarAsync(new CrearCategoriaRequest(NuevaCategoriaNombre));
            await CargarCategoriasAsync();
            CategoriaSeleccionada = CategoriasDisponibles.FirstOrDefault(x => x.Id == categoria.Id);
            CerrarModalCategoria();
            _ = MostrarNotificacionAsync("✓ Categoría creada correctamente.");
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Categoría", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task CargarProductosAsync()
    {
        var activos = (await _productoRepository.ObtenerActivosAsync()).ToList();
        var inactivos = (await _productoRepository.ObtenerInactivosAsync()).ToList();

        _productosOriginales = activos.Concat(inactivos).ToList();
        TotalProductos = _productosOriginales.Count;
        TotalProductosActivos = activos.Count;
        TotalProductosInactivos = inactivos.Count;
        ProductosConStockBajo = activos.Count(x =>
            x.StockMinimo > 0 && x.StockActual <= x.StockMinimo);

        AplicarFiltros();
    }

    private void PrepararNuevoProducto()
    {
        _idProductoEdicion = 0;
        TituloFormulario = "Nuevo producto";
        OnPropertyChanged(nameof(TextoAccionFormulario));
        FormularioHabilitado = true;
        ProductoSeleccionado = null;
        Nombre = string.Empty;
        CategoriaSeleccionada = null;
        CodigoBarras = null;
        PrecioVenta = 0;
        UnidadVenta = UnidadVenta.Unidad;
        StockActual = 0;
        StockMinimo = 0;
    }

    private void CargarFormularioProducto(Producto producto)
    {
        _idProductoEdicion = producto.Id;
        TituloFormulario = producto.Activo ? "Editar producto" : "Producto desactivado";
        OnPropertyChanged(nameof(TextoAccionFormulario));
        FormularioHabilitado = producto.Activo;
        Nombre = producto.Nombre;
        CategoriaSeleccionada = CategoriasDisponibles.FirstOrDefault(x => x.Id == producto.CategoriaId);
        CodigoBarras = producto.CodigoBarras;
        PrecioVenta = producto.PrecioVenta;
        UnidadVenta = producto.UnidadVenta;
        StockActual = producto.StockActual;
        StockMinimo = producto.StockMinimo;
    }
}
