using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Ventas.UseCases;
using BodegaLuchito.Application.Ventas.DTOs;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Ventas.ViewModels;

public class DetalleCarritoVenta : ViewModelBase
{
    private decimal _cantidad;
    private decimal _precioUnitario;

    public int ProductoId { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public UnidadVenta UnidadVenta { get; set; }

    public decimal Cantidad
    {
        get => _cantidad;
        set
        {
            if (value < 0) return;

            if (UnidadVenta == UnidadVenta.Unidad && value % 1 != 0)
            {
                _cantidad = Math.Round(value, 0);
            }
            else
            {
                _cantidad = value;
            }
            OnPropertyChanged();
            OnPropertyChanged(nameof(Subtotal));
        }
    }

    public decimal PrecioUnitario
    {
        get => _precioUnitario;
        set
        {
            _precioUnitario = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Subtotal));
        }
    }

    public decimal Subtotal => Math.Round(Cantidad * PrecioUnitario, 2);
}

public partial class VentasViewModel : ViewModelBase
{
    private readonly IProductoRepository _productoRepository;
    private readonly RegistrarVentaUseCase _registrarVentaUseCase;
    private readonly ISesionUsuario _sesionUsuario;

    private List<Producto> _todosLosProductos = new();

    public ObservableCollection<Producto> ProductosFiltrados { get; } = new();
    public ObservableCollection<DetalleCarritoVenta> Carrito { get; } = new();

    private string _textoBusquedaProducto = string.Empty;
    public string TextoBusquedaProducto
    {
        get => _textoBusquedaProducto;
        set
        {
            SetProperty(ref _textoBusquedaProducto, value);
            ProcesarBusquedaOEscaneo();
        }
    }

    private Producto? _productoSeleccionado;
    public Producto? ProductoSeleccionado
    {
        get => _productoSeleccionado;
        set => SetProperty(ref _productoSeleccionado, value);
    }

    private bool _isDropdownOpen;
    public bool IsDropdownOpen
    {
        get => _isDropdownOpen;
        set => SetProperty(ref _isDropdownOpen, value);
    }

    private Visibility _modalCobroVisible = Visibility.Collapsed;
    public Visibility ModalCobroVisible
    {
        get => _modalCobroVisible;
        set => SetProperty(ref _modalCobroVisible, value);
    }

    public decimal TotalVenta => Carrito.Sum(x => x.Subtotal);

    public VentasViewModel(
        IProductoRepository productoRepository,
        RegistrarVentaUseCase registrarVentaUseCase,
        ISesionUsuario sesionUsuario)
    {
        _productoRepository = productoRepository;
        _registrarVentaUseCase = registrarVentaUseCase;
        _sesionUsuario = sesionUsuario;
        CargarCatalogosAsync();
    }

    private async void CargarCatalogosAsync()
    {
        var productos = await _productoRepository.ObtenerActivosAsync();
        _todosLosProductos = productos.ToList();
    }

    private void ProcesarBusquedaOEscaneo()
    {
        if (string.IsNullOrWhiteSpace(TextoBusquedaProducto))
        {
            ProductosFiltrados.Clear();
            IsDropdownOpen = false;
            return;
        }

        var productoEscaneado = _todosLosProductos.FirstOrDefault(p =>
            p.CodigoBarras != null &&
            p.CodigoBarras.Equals(TextoBusquedaProducto.Trim(), StringComparison.OrdinalIgnoreCase));

        if (productoEscaneado != null)
        {
            AgregarProductoAlCarrito(productoEscaneado);
            TextoBusquedaProducto = string.Empty;
            IsDropdownOpen = false;
            return;
        }

        FiltrarProductos();
    }

    private void FiltrarProductos()
    {
        ProductosFiltrados.Clear();
        var filtrados = _todosLosProductos.Where(p =>
            (p.Nombre != null && p.Nombre.Contains(TextoBusquedaProducto, StringComparison.OrdinalIgnoreCase)) ||
            (p.CodigoBarras != null && p.CodigoBarras.Contains(TextoBusquedaProducto, StringComparison.OrdinalIgnoreCase))
        ).Take(10).ToList();

        foreach (var prod in filtrados)
        {
            ProductosFiltrados.Add(prod);
        }

        IsDropdownOpen = ProductosFiltrados.Any();
        ProductoSeleccionado = ProductosFiltrados.FirstOrDefault();
    }

    [RelayCommand]
    private void AgregarAlCarritoManual()
    {
        if (ProductoSeleccionado != null)
        {
            AgregarProductoAlCarrito(ProductoSeleccionado);
            TextoBusquedaProducto = string.Empty;
            IsDropdownOpen = false;
        }
    }

    private void AgregarProductoAlCarrito(Producto producto)
    {
        var existe = Carrito.FirstOrDefault(x => x.ProductoId == producto.Id);
        if (existe != null)
        {
            existe.Cantidad += 1;
        }
        else
        {
            Carrito.Add(new DetalleCarritoVenta
            {
                ProductoId = producto.Id,
                CodigoBarras = producto.CodigoBarras ?? "S/C",
                Nombre = producto.Nombre,
                UnidadVenta = producto.UnidadVenta,
                PrecioUnitario = producto.PrecioVenta,
                Cantidad = 1
            });
        }
        OnPropertyChanged(nameof(TotalVenta));
    }

    [RelayCommand]
    private void QuitarDelCarrito(DetalleCarritoVenta detalle)
    {
        if (detalle != null && Carrito.Contains(detalle))
        {
            Carrito.Remove(detalle);
            OnPropertyChanged(nameof(TotalVenta));
        }
    }

    [RelayCommand]
    private void IrACobrar()
    {
        if (!Carrito.Any())
        {
            MessageBox.Show("El carrito está vacío. Agregue productos antes de cobrar.", "Bodega Luchito", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ModalCobroVisible = Visibility.Visible;
    }

    [RelayCommand]
    private void CancelarCobro() => ModalCobroVisible = Visibility.Collapsed;

    [RelayCommand]
    private async Task CobrarEfectivoAsync() => await ProcesarCobroAsync(MetodoPago.Efectivo);

    [RelayCommand]
    private async Task CobrarYapeAsync() => await ProcesarCobroAsync(MetodoPago.Yape);

    [RelayCommand]
    private async Task CobrarPlinAsync() => await ProcesarCobroAsync(MetodoPago.Plin);

    private async Task ProcesarCobroAsync(MetodoPago metodoPago)
    {
        try
        {
            decimal subtotalCalc = Math.Round(TotalVenta / 1.18m, 2);
            decimal igvCalc = TotalVenta - subtotalCalc;

            var request = new RegistrarVentaRequest
            {
                UsuarioId = _sesionUsuario.UsuarioActual!.IdUsuario,
                Total = TotalVenta,
                Subtotal = subtotalCalc,
                IGV = igvCalc,
                MetodoPago = metodoPago,

                Detalles = Carrito.Select(item => new DetalleVentaRequest
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = item.Subtotal
                }).ToList()
            };

            int nuevaVentaId = await _registrarVentaUseCase.ExecuteAsync(request);

            var ventanaTicket = new BodegaLuchito.Desktop.Modules.Ventas.Views.TicketVentaWindow(
                nuevaVentaId,
                _sesionUsuario.UsuarioActual!.NombreCompleto,
                metodoPago,
                subtotalCalc,
                igvCalc,
                TotalVenta,
                Carrito.ToList() 
            );
            ventanaTicket.ShowDialog();

            MessageBox.Show($"¡Venta registrada!\nSe guardó la operación con ID: {nuevaVentaId}", "Bodega Luchito", MessageBoxButton.OK, MessageBoxImage.Information);

            // Reiniciamos la pantalla para el siguiente cliente
            ModalCobroVisible = Visibility.Collapsed;
            Carrito.Clear();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error al procesar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
