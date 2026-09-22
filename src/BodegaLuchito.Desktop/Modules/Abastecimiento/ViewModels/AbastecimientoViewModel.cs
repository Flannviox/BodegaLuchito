using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using BodegaLuchito.Application.Abastecimiento.DTOs;
using BodegaLuchito.Application.Abastecimiento.UseCases;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Proveedores.Entities;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Abastecimiento.ViewModels;

public class DetalleCarrito : ViewModelBase
{
    private decimal _cantidad;
    private decimal _precioUnitario;

    public int ProductoId { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public string Categoria { get; set; } = string.Empty;
    public string CodigoBarras { get; set; } = string.Empty;

    public decimal Cantidad
    {
        get => _cantidad;
        set { _cantidad = value; OnPropertyChanged(); OnPropertyChanged(nameof(Subtotal)); }
    }

    public decimal PrecioUnitario
    {
        get => _precioUnitario;
        set { _precioUnitario = value; OnPropertyChanged(); OnPropertyChanged(nameof(Subtotal)); }
    }

    public decimal Subtotal => Cantidad * PrecioUnitario;
}

public partial class AbastecimientoViewModel : ViewModelBase
{
    private readonly RegistrarAbastecimientoUseCase _registrarUseCase;
    private readonly IProveedorRepository _proveedorRepository;
    private readonly IProductoRepository _productoRepository;
    private readonly ISesionUsuario _sesionUsuario;

    // Listas maestras en memoria para búsqueda ultra rápida
    private List<Proveedor> _todosLosProveedores = new();
    private List<Producto> _todosLosProductos = new();

    // Listas observables que se muestran en las tablas (DataGrids)
    public ObservableCollection<Proveedor> ProveedoresFiltrados { get; } = new();
    public ObservableCollection<Producto> ProductosFiltrados { get; } = new();
    public ObservableCollection<DetalleCarrito> Carrito { get; } = new();

    public IEnumerable<MetodoPago> MetodosPago => Enum.GetValues<MetodoPago>();

    // Búsqueda en vivo: Proveedores
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

    // Búsqueda en vivo: Productos
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
        set => SetProperty(ref _productoSeleccionado, value);
    }

    private decimal _cantidadIngreso = 1;
    public decimal CantidadIngreso
    {
        get => _cantidadIngreso;
        set => SetProperty(ref _cantidadIngreso, value);
    }

    private decimal _precioCompra;
    public decimal PrecioCompra
    {
        get => _precioCompra;
        set => SetProperty(ref _precioCompra, value);
    }

    public decimal TotalAbastecimiento => Carrito.Sum(x => x.Subtotal);

    // Notificaciones Snackbar
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
        IProveedorRepository proveedorRepository,
        IProductoRepository productoRepository,
        ISesionUsuario sesionUsuario)
    {
        _registrarUseCase = registrarUseCase;
        _proveedorRepository = proveedorRepository;
        _productoRepository = productoRepository;
        _sesionUsuario = sesionUsuario;

        CargarCatalogosAsync();
    }

    private async void CargarCatalogosAsync()
    {
        // Traer proveedores a memoria
        var provs = await _proveedorRepository.ObtenerTodosAsync();
        _todosLosProveedores = provs.Where(p => p.Activo).ToList();
        FiltrarProveedores();

        // Traer productos a memoria
        var prods = await _productoRepository.ObtenerActivosAsync();
        _todosLosProductos = prods.ToList();
        FiltrarProductos();
    }

    private void FiltrarProveedores()
    {
        ProveedoresFiltrados.Clear();
        var filtrados = string.IsNullOrWhiteSpace(TextoBusquedaProveedor)
            ? _todosLosProveedores
            : _todosLosProveedores.Where(p =>
                (p.Nombre != null && p.Nombre.Contains(TextoBusquedaProveedor, StringComparison.OrdinalIgnoreCase)) ||
                (p.Ruc != null && p.Ruc.Contains(TextoBusquedaProveedor, StringComparison.OrdinalIgnoreCase))
            ).ToList();

        foreach (var p in filtrados) ProveedoresFiltrados.Add(p);
    }

    private void FiltrarProductos()
    {
        ProductosFiltrados.Clear();
        var filtrados = string.IsNullOrWhiteSpace(TextoBusquedaProducto)
            ? _todosLosProductos
            : _todosLosProductos.Where(p =>
                (p.Nombre != null && p.Nombre.Contains(TextoBusquedaProducto, StringComparison.OrdinalIgnoreCase)) ||
                (p.CodigoBarras != null && p.CodigoBarras.Contains(TextoBusquedaProducto, StringComparison.OrdinalIgnoreCase))
            ).ToList();

        foreach (var prod in filtrados) ProductosFiltrados.Add(prod);
    }

    [RelayCommand]
    private void AgregarAlCarrito()
    {
        if (ProductoSeleccionado == null)
        {
            MostrarMensaje("Seleccione un producto de la tabla.");
            return;
        }
        if (CantidadIngreso <= 0 || PrecioCompra <= 0)
        {
            MostrarMensaje("La cantidad y el precio deben ser mayores a cero.");
            return;
        }

        var existe = Carrito.FirstOrDefault(x => x.ProductoId == ProductoSeleccionado.Id);
        if (existe != null)
        {
            existe.Cantidad += CantidadIngreso;
            existe.PrecioUnitario = PrecioCompra;
        }
        else
        {
            Carrito.Add(new DetalleCarrito
            {
                ProductoId = ProductoSeleccionado.Id,
                NombreProducto = ProductoSeleccionado.Nombre,
                CodigoBarras = ProductoSeleccionado.CodigoBarras ?? "S/C",
                Categoria = ProductoSeleccionado.Categoria?.Nombre ?? "Sin categoría",
                Cantidad = CantidadIngreso,
                PrecioUnitario = PrecioCompra
            });
        }

        OnPropertyChanged(nameof(TotalAbastecimiento));
        CantidadIngreso = 1;
        PrecioCompra = 0;
        TextoBusquedaProducto = string.Empty; // Limpia el buscador y resetea la tabla
    }

    [RelayCommand]
    private void QuitarDelCarrito(DetalleCarrito detalle)
    {
        if (detalle != null && Carrito.Contains(detalle))
        {
            Carrito.Remove(detalle);
            OnPropertyChanged(nameof(TotalAbastecimiento));
        }
    }

    [RelayCommand]
    private async Task ConfirmarAbastecimientoAsync()
    {
        if (ProveedorSeleccionado == null)
        {
            MostrarMensaje("Debe seleccionar un proveedor de la tabla.");
            return;
        }
        if (!Carrito.Any())
        {
            MostrarMensaje("El carrito de abastecimiento está vacío.");
            return;
        }

        var request = new RegistrarAbastecimientoRequest
        {
            ProveedorId = ProveedorSeleccionado.Id,
            Total = TotalAbastecimiento,
            MetodoPago = MetodoPagoSeleccionado,
            UsuarioId = 1, // Bypass temporal
            Detalles = Carrito.Select(c => new DetalleAbastecimientoRequest
            {
                ProductoId = c.ProductoId,
                Cantidad = c.Cantidad,
                PrecioUnitario = c.PrecioUnitario
            }).ToList()
        };

        try
        {
            await _registrarUseCase.ExecuteAsync(request);
            Carrito.Clear();
            ProveedorSeleccionado = null;
            TextoBusquedaProveedor = string.Empty;
            OnPropertyChanged(nameof(TotalAbastecimiento));
            MostrarMensaje("¡Abastecimiento registrado con éxito!");
        }
        catch (Exception ex)
        {
            MostrarMensaje($"Error: {ex.Message}");
        }
    }

    private async void MostrarMensaje(string mensaje)
    {
        MensajeSnackbar = mensaje;
        IsSnackbarActive = true;
        await Task.Delay(3000);
        IsSnackbarActive = false;
    }
}
