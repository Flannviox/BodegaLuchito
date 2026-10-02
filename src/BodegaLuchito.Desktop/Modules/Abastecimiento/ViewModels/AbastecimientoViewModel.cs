using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BodegaLuchito.Application.Abastecimiento.DTOs;
using BodegaLuchito.Application.Abastecimiento.UseCases;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Application.Inventario.UseCases;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.Input;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Desktop.Modules.Productos.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Abastecimiento.ViewModels;

public sealed class DetalleAbastecimientoItem : ViewModelBase
{
    public RegistrarProductoRequest? ProductoNuevo { get; set; }
    private decimal _cantidad;
    private decimal _costoUnitario;
    private decimal _totalLinea;

    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;

    public decimal Cantidad
    {
        get => _cantidad;
        set
        {
            _cantidad = value;
            OnPropertyChanged();
        }
    }

    public decimal CostoUnitario
    {
        get => _costoUnitario;
        set
        {
            _costoUnitario = value;
            OnPropertyChanged();
        }
    }

    public decimal TotalLinea
    {
        get => _totalLinea;
        set
        {
            _totalLinea = value;
            OnPropertyChanged();
        }
    }
}

public sealed class MovimientoInventarioItem
{
    public DateTime FechaHora { get; init; }
    public string NombreProducto { get; init; } = string.Empty;
    public string TipoDescripcion { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal StockAnterior { get; init; }
    public decimal StockPosterior { get; init; }
    public int? AbastecimientoId { get; init; }
}

public sealed class DetalleHistorialAbastecimientoItem
{
    public string NombreProducto { get; init; } = string.Empty;
    public decimal Cantidad { get; init; }
    public decimal CostoUnitario { get; init; }
    public decimal CostoTotal { get; init; }
}

public sealed class HistorialAbastecimientoItem
{
    public int Id { get; init; }
    public DateTime FechaHora { get; init; }
    public string Proveedor { get; init; } = string.Empty;
    public MetodoPago MetodoPago { get; init; }
    public decimal Total { get; init; }
    public IReadOnlyList<DetalleHistorialAbastecimientoItem> Detalles { get; init; } = [];
}

public partial class AbastecimientoViewModel : ViewModelBase
{
    private const string CostoPorUnidad = "Costo por unidad";
    private const string CostoTotalLote = "Costo total del lote";

    private readonly RegistrarAbastecimientoUseCase _registrarUseCase;
    private readonly ConsultarHistorialAbastecimientosUseCase _consultarHistorialUseCase;
    private readonly ConsultarMovimientosInventarioUseCase _consultarMovimientosUseCase;
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly ISesionUsuario _sesionUsuario;
    public NuevoProductoIngresoViewModel AltaProducto { get; }
    private int _siguienteIdTemporal = -1;

    private List<Proveedor> _todosLosProveedores = new();
    private List<Producto> _todosLosProductos = new();

    public ObservableCollection<Proveedor> ProveedoresFiltrados { get; } = new();
    public ObservableCollection<Producto> ProductosFiltrados { get; } = new();
    public ObservableCollection<DetalleAbastecimientoItem> ProductosAbastecimiento { get; } = new();
    public ObservableCollection<MovimientoInventarioItem> MovimientosInventario { get; } = new();
    public ObservableCollection<HistorialAbastecimientoItem> HistorialAbastecimientos { get; } = new();
    public ObservableCollection<DetalleHistorialAbastecimientoItem> DetallesHistorialAbastecimiento { get; } = new();

    public IEnumerable<MetodoPago> MetodosPago => Enum.GetValues<MetodoPago>();
    public IReadOnlyList<string> ModosCosto { get; } = [CostoPorUnidad, CostoTotalLote];

    private DetalleAbastecimientoItem? _detalleEnEdicion;
    public bool EstaEditando => _detalleEnEdicion is not null;
    public string TextoBotonProducto => EstaEditando
        ? "Actualizar producto"
        : "+ Agregar al abastecimiento";

    private HistorialAbastecimientoItem? _abastecimientoHistorialSeleccionado;
    public HistorialAbastecimientoItem? AbastecimientoHistorialSeleccionado
    {
        get => _abastecimientoHistorialSeleccionado;
        set
        {
            SetProperty(ref _abastecimientoHistorialSeleccionado, value);
            CargarDetallesHistorial();
        }
    }

    private string _textoBusquedaProveedor = string.Empty;
    public string TextoBusquedaProveedor
    {
        get => _textoBusquedaProveedor;
        set
        {
            SetProperty(ref _textoBusquedaProveedor, value);
            FiltrarProveedores();
        }
    }

    private string _textoBusquedaProducto = string.Empty;
    public string TextoBusquedaProducto
    {
        get => _textoBusquedaProducto;
        set
        {
            SetProperty(ref _textoBusquedaProducto, value);
            FiltrarProductos();
        }
    }

    private Proveedor? _proveedorSeleccionado;
    public Proveedor? ProveedorSeleccionado
    {
        get => _proveedorSeleccionado;
        set => SetProperty(ref _proveedorSeleccionado, value);
    }

    private MetodoPago _metodoPagoSeleccionado = MetodoPago.Efectivo;
    public MetodoPago MetodoPagoSeleccionado
    {
        get => _metodoPagoSeleccionado;
        set => SetProperty(ref _metodoPagoSeleccionado, value);
    }

    private Producto? _productoSeleccionado;
    public Producto? ProductoSeleccionado
    {
        get => _productoSeleccionado;
        set
        {
            if (SetProperty(ref _productoSeleccionado, value))
                OnPropertyChanged(nameof(EtiquetaCantidad));
        }
    }

    private decimal _cantidadIngreso = 1;
    public decimal CantidadIngreso
    {
        get => _cantidadIngreso;
        set
        {
            SetProperty(ref _cantidadIngreso, value);
            OnPropertyChanged(nameof(VistaPreviaCostoUnitario));
            OnPropertyChanged(nameof(VistaPreviaTotalLinea));
        }
    }

    private string _modoCostoSeleccionado = CostoPorUnidad;
    public string ModoCostoSeleccionado
    {
        get => _modoCostoSeleccionado;
        set
        {
            SetProperty(ref _modoCostoSeleccionado, value);
            CostoIngresado = _costoIngresado;
            OnPropertyChanged(nameof(EtiquetaCostoIngresado));
            OnPropertyChanged(nameof(VistaPreviaCostoUnitario));
            OnPropertyChanged(nameof(VistaPreviaTotalLinea));
        }
    }

    private decimal _costoIngresado;
    public decimal CostoIngresado
    {
        get => _costoIngresado;
        set
        {
            var decimalPlaces = ModoCostoSeleccionado == CostoPorUnidad ? 4 : 2;
            SetProperty(ref _costoIngresado, Math.Round(value, decimalPlaces, MidpointRounding.AwayFromZero));
            OnPropertyChanged(nameof(VistaPreviaCostoUnitario));
            OnPropertyChanged(nameof(VistaPreviaTotalLinea));
        }
    }

    public string EtiquetaCostoIngresado => ModoCostoSeleccionado == CostoPorUnidad
        ? "Costo por unidad (S/)"
        : "Costo total del lote (S/)";

    public string EtiquetaCantidad => ProductoSeleccionado?.UnidadVenta == UnidadVenta.Peso
        ? "Cantidad (kg)"
        : "Cantidad (unidades)";

    public decimal VistaPreviaCostoUnitario => CalcularCostoUnitario();
    public decimal VistaPreviaTotalLinea => CalcularTotalLinea();
    public decimal TotalAbastecimiento => ProductosAbastecimiento.Sum(x => x.TotalLinea);
    public double AlturaTablaProductos =>
        Math.Min(Math.Max(ProductosAbastecimiento.Count, 1), 4) * 36d + 48d;

    private string _mensajeSnackbar = string.Empty;
    public string MensajeSnackbar
    {
        get => _mensajeSnackbar;
        set => SetProperty(ref _mensajeSnackbar, value);
    }

    private bool _isSnackbarActive;
    public bool IsSnackbarActive
    {
        get => _isSnackbarActive;
        set => SetProperty(ref _isSnackbarActive, value);
    }

    public AbastecimientoViewModel(
        RegistrarAbastecimientoUseCase registrarUseCase,
        ConsultarHistorialAbastecimientosUseCase consultarHistorialUseCase,
        ConsultarMovimientosInventarioUseCase consultarMovimientosUseCase,
        IProveedorRepository proveedorRepository,
        IProductoRepository productoRepository,
        ISesionUsuario sesionUsuario,
        ICategoriaRepository categorias)
    {
        _registrarUseCase = registrarUseCase;
        _consultarHistorialUseCase = consultarHistorialUseCase;
        _consultarMovimientosUseCase = consultarMovimientosUseCase;
        _proveedorRepository = proveedorRepository;
        _productoRepository = productoRepository;
        _sesionUsuario = sesionUsuario;
        AltaProducto = new NuevoProductoIngresoViewModel(categorias);

        _ = CargarDatosInicialesAsync();
    }

    private async Task CargarDatosInicialesAsync()
    {
        await CargarCatalogosAsync();
        await CargarMovimientosAsync();
        await CargarHistorialAbastecimientosAsync();
    }

    private async Task CargarCatalogosAsync()
    {
        var proveedores = await _proveedorRepository.ObtenerTodosAsync();
        _todosLosProveedores = proveedores.Where(x => x.Activo).ToList();
        FiltrarProveedores();

        var productos = await _productoRepository.ObtenerActivosAsync();
        _todosLosProductos = productos.ToList();
        FiltrarProductos();
    }

    private async Task CargarMovimientosAsync()
    {
        var movimientos = await _consultarMovimientosUseCase.ExecuteAsync(
            tipo: BodegaLuchito.Domain.Inventario.Enums.TipoMovimientoInventario.EntradaAbastecimiento);
        MovimientosInventario.Clear();

        foreach (var movimiento in movimientos)
        {
            MovimientosInventario.Add(new MovimientoInventarioItem
            {
                FechaHora = movimiento.FechaHora,
                NombreProducto = movimiento.Producto?.Nombre ?? "Producto no disponible",
                TipoDescripcion = movimiento.Tipo switch
                {
                    BodegaLuchito.Domain.Inventario.Enums.TipoMovimientoInventario.EntradaAbastecimiento => "Ingreso por abastecimiento",
                    _ => movimiento.Tipo.ToString()
                },
                Cantidad = movimiento.Cantidad,
                StockAnterior = movimiento.StockAnterior,
                StockPosterior = movimiento.StockPosterior,
                AbastecimientoId = movimiento.AbastecimientoId
            });
        }
    }

    private async Task CargarHistorialAbastecimientosAsync()
    {
        var abastecimientos = await _consultarHistorialUseCase.ExecuteAsync();
        HistorialAbastecimientos.Clear();
        AbastecimientoHistorialSeleccionado = null;

        foreach (var abastecimiento in abastecimientos)
        {
            HistorialAbastecimientos.Add(new HistorialAbastecimientoItem
            {
                Id = abastecimiento.Id,
                FechaHora = abastecimiento.FechaHora,
                Proveedor = abastecimiento.Proveedor.Nombre,
                MetodoPago = abastecimiento.MetodoPago,
                Total = abastecimiento.Total,
                Detalles = abastecimiento.Detalles.Select(detalle => new DetalleHistorialAbastecimientoItem
                {
                    NombreProducto = detalle.Producto.Nombre,
                    Cantidad = detalle.Cantidad,
                    CostoUnitario = detalle.CostoUnitario,
                    CostoTotal = detalle.TotalLinea
                }).ToList()
            });
        }
    }

    private void CargarDetallesHistorial()
    {
        DetallesHistorialAbastecimiento.Clear();

        if (AbastecimientoHistorialSeleccionado is null)
            return;

        foreach (var detalle in AbastecimientoHistorialSeleccionado.Detalles)
            DetallesHistorialAbastecimiento.Add(detalle);
    }

    private void FiltrarProveedores()
    {
        ProveedoresFiltrados.Clear();
        var filtrados = string.IsNullOrWhiteSpace(TextoBusquedaProveedor)
            ? _todosLosProveedores
            : _todosLosProveedores.Where(x =>
                x.Nombre.Contains(TextoBusquedaProveedor, StringComparison.OrdinalIgnoreCase) ||
                x.Ruc.Contains(TextoBusquedaProveedor, StringComparison.OrdinalIgnoreCase));

        foreach (var proveedor in filtrados)
            ProveedoresFiltrados.Add(proveedor);
    }

    private void FiltrarProductos()
    {
        ProductosFiltrados.Clear();
        var filtrados = string.IsNullOrWhiteSpace(TextoBusquedaProducto)
            ? _todosLosProductos
            : _todosLosProductos.Where(x =>
                x.Nombre.Contains(TextoBusquedaProducto, StringComparison.OrdinalIgnoreCase) ||
                (x.CodigoBarras?.Contains(TextoBusquedaProducto, StringComparison.OrdinalIgnoreCase) ?? false));

        foreach (var producto in filtrados)
            ProductosFiltrados.Add(producto);
    }

    [RelayCommand]
    private void AgregarAlAbastecimiento()
    {
        if (ProductoSeleccionado is null)
        {
            MostrarMensaje("Seleccione un producto de la tabla.");
            return;
        }

        if (CantidadIngreso <= 0 || CostoIngresado <= 0)
        {
            MostrarMensaje("La cantidad y el costo deben ser mayores a cero.");
            return;
        }

        var costoUnitario = CalcularCostoUnitario();
        var totalLinea = CalcularTotalLinea();

        if (_detalleEnEdicion is not null)
        {
            if (ProductoSeleccionado.Id != _detalleEnEdicion.ProductoId)
            {
                MostrarMensaje("Para cambiar de producto, cancele la edición y agregue una nueva línea.");
                return;
            }

            _detalleEnEdicion.Cantidad = CantidadIngreso;
            _detalleEnEdicion.CostoUnitario = costoUnitario;
            _detalleEnEdicion.TotalLinea = totalLinea;
            OnPropertyChanged(nameof(TotalAbastecimiento));
            RestablecerFormularioProducto();
            MostrarMensaje("Producto actualizado en el abastecimiento.");
            return;
        }

        var detalleExistente = ProductosAbastecimiento.FirstOrDefault(x => x.ProductoId == ProductoSeleccionado.Id);

        if (detalleExistente is not null)
        {
            detalleExistente.Cantidad += CantidadIngreso;
            detalleExistente.TotalLinea = Math.Round(
                detalleExistente.TotalLinea + totalLinea,
                2,
                MidpointRounding.AwayFromZero);
            detalleExistente.CostoUnitario = Math.Round(
                detalleExistente.TotalLinea / detalleExistente.Cantidad,
                4,
                MidpointRounding.AwayFromZero);
        }
        else
        {
            ProductosAbastecimiento.Add(new DetalleAbastecimientoItem
            {
                ProductoId = ProductoSeleccionado.Id,
                NombreProducto = ProductoSeleccionado.Nombre,
                CodigoBarras = ProductoSeleccionado.CodigoBarras ?? "S/C",
                Categoria = ProductoSeleccionado.Categoria?.Nombre ?? "Sin categoría",
                Cantidad = CantidadIngreso,
                CostoUnitario = costoUnitario,
                TotalLinea = totalLinea
            });
        }

        OnPropertyChanged(nameof(TotalAbastecimiento));
        OnPropertyChanged(nameof(AlturaTablaProductos));
        RestablecerFormularioProducto();
    }

    [RelayCommand]
    private async Task NuevoProductoAsync() => await AbrirAltaProductoAsync();

    private Task AbrirAltaProductoAsync(DetalleAbastecimientoItem? editar = null) =>
        AltaProducto.AbrirAsync(true, async (datos, cantidad, unitario, total) =>
        {
            var codigo = string.IsNullOrWhiteSpace(datos.CodigoBarras) ? null : datos.CodigoBarras.Trim();
            if (codigo is not null &&
                (await _productoRepository.ExisteCodigoBarrasAsync(codigo) ||
                 ProductosAbastecimiento.Any(x => x != editar && x.ProductoNuevo?.CodigoBarras?.Trim() == codigo)))
                throw new InvalidOperationException("Este código ya pertenece a un producto o está pendiente en esta compra.");
            var item = new DetalleAbastecimientoItem {
                ProductoId = editar?.ProductoId ?? _siguienteIdTemporal--, ProductoNuevo = datos,
                NombreProducto = datos.Nombre.Trim() + " (nuevo)", CodigoBarras = codigo ?? "S/C",
                Categoria = AltaProducto.Categoria?.Nombre ?? "",
                Cantidad = cantidad, CostoUnitario = unitario, TotalLinea = total
            };
            if (editar is null) ProductosAbastecimiento.Add(item);
            else ProductosAbastecimiento[ProductosAbastecimiento.IndexOf(editar)] = item;
            OnPropertyChanged(nameof(TotalAbastecimiento));
            OnPropertyChanged(nameof(AlturaTablaProductos));
            MostrarMensaje("Producto pendiente. Se guardará al confirmar el abastecimiento.");
        }, editar?.ProductoNuevo, editar?.Cantidad, editar?.TotalLinea);

    [RelayCommand]
    private async Task EditarProductoAbastecimientoAsync(DetalleAbastecimientoItem? detalle)
    {
        if (detalle is null || !ProductosAbastecimiento.Contains(detalle))
            return;

        if (detalle.ProductoNuevo is not null)
        {
            await AbrirAltaProductoAsync(detalle);
            return;
        }

        var producto = _todosLosProductos.FirstOrDefault(x => x.Id == detalle.ProductoId);

        if (producto is null)
        {
            MostrarMensaje("El producto ya no está disponible para editar.");
            return;
        }

        _detalleEnEdicion = detalle;
        ProductoSeleccionado = producto;
        CantidadIngreso = detalle.Cantidad;
        ModoCostoSeleccionado = CostoTotalLote;
        CostoIngresado = detalle.TotalLinea;
        OnPropertyChanged(nameof(EstaEditando));
        OnPropertyChanged(nameof(TextoBotonProducto));
    }

    [RelayCommand]
    private void CancelarEdicion()
    {
        if (!EstaEditando)
            return;

        RestablecerFormularioProducto();
    }

    [RelayCommand]
    private void QuitarProductoAbastecimiento(DetalleAbastecimientoItem? detalle)
    {
        if (detalle is null || !ProductosAbastecimiento.Remove(detalle))
            return;

        if (ReferenceEquals(detalle, _detalleEnEdicion))
            RestablecerFormularioProducto();

        OnPropertyChanged(nameof(TotalAbastecimiento));
        OnPropertyChanged(nameof(AlturaTablaProductos));
    }

    [RelayCommand]
    private async Task ConfirmarAbastecimientoAsync()
    {
        if (ProveedorSeleccionado is null)
        {
            MostrarMensaje("Debe seleccionar un proveedor de la tabla.");
            return;
        }

        if (!ProductosAbastecimiento.Any())
        {
            MostrarMensaje("Agregue al menos un producto al abastecimiento.");
            return;
        }

        var usuarioActual = _sesionUsuario.UsuarioActual;

        if (usuarioActual is null)
        {
            MostrarMensaje("No existe un usuario autenticado.");
            return;
        }

        var request = new RegistrarAbastecimientoRequest
        {
            ProveedorId = ProveedorSeleccionado.Id,
            MetodoPago = MetodoPagoSeleccionado,
            UsuarioId = usuarioActual.IdUsuario,
            Detalles = ProductosAbastecimiento.Select(x => new DetalleAbastecimientoRequest
            {
                ProductoId = x.ProductoNuevo is null ? x.ProductoId : 0,
                ProductoNuevo = x.ProductoNuevo,
                Cantidad = x.Cantidad,
                CostoUnitario = x.CostoUnitario,
                TotalLinea = x.TotalLinea
            }).ToList()
        };

        try
        {
            await _registrarUseCase.ExecuteAsync(request);
            ProductosAbastecimiento.Clear();
            OnPropertyChanged(nameof(AlturaTablaProductos));
            RestablecerFormularioProducto();
            ProveedorSeleccionado = null;
            TextoBusquedaProveedor = string.Empty;
            OnPropertyChanged(nameof(TotalAbastecimiento));
            await CargarCatalogosAsync();
            await CargarMovimientosAsync();
            await CargarHistorialAbastecimientosAsync();
            MostrarMensaje("Ingreso de mercadería registrado con éxito.");
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error: {ex.Message}");
        }
    }

    private decimal CalcularCostoUnitario()
    {
        if (CantidadIngreso <= 0 || CostoIngresado <= 0)
            return 0;

        return ModoCostoSeleccionado == CostoPorUnidad
            ? Math.Round(CostoIngresado, 4, MidpointRounding.AwayFromZero)
            : Math.Round(CostoIngresado / CantidadIngreso, 4, MidpointRounding.AwayFromZero);
    }

    private decimal CalcularTotalLinea()
    {
        if (CantidadIngreso <= 0 || CostoIngresado <= 0)
            return 0;

        return ModoCostoSeleccionado == CostoPorUnidad
            ? Math.Round(CantidadIngreso * CostoIngresado, 2, MidpointRounding.AwayFromZero)
            : Math.Round(CostoIngresado, 2, MidpointRounding.AwayFromZero);
    }

    private void RestablecerFormularioProducto()
    {
        _detalleEnEdicion = null;
        OnPropertyChanged(nameof(EstaEditando));
        OnPropertyChanged(nameof(TextoBotonProducto));
        ProductoSeleccionado = null;
        CantidadIngreso = 1;
        ModoCostoSeleccionado = CostoPorUnidad;
        CostoIngresado = 0;
        TextoBusquedaProducto = string.Empty;
    }

    private async void MostrarMensaje(string mensaje)
    {
        MensajeSnackbar = mensaje;
        IsSnackbarActive = true;
        await Task.Delay(3000);
        IsSnackbarActive = false;
    }
}
