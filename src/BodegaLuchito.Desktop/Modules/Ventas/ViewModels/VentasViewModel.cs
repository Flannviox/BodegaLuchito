using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Ventas.Interfaces;
using BodegaLuchito.Application.Ventas.UseCases;
using BodegaLuchito.Application.Ventas.DTOs;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using BodegaLuchito.Domain.Shared.Enums;
using BodegaLuchito.Domain.Ventas.Entities;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Ventas.ViewModels;

public class DetalleCarritoVenta : ViewModelBase
{
    private decimal _cantidad;
    private string _cantidadTexto = "1";
    private decimal _precioUnitario;

    public int ProductoId { get; set; }
    public string CodigoBarras { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public UnidadVenta UnidadVenta { get; set; }

    // PROPIEDAD PARA LA CAJA DE TEXTO (Protege la escritura de puntos y comas)
    public string CantidadTexto
    {
        get => _cantidadTexto;
        set
        {
            _cantidadTexto = value;
            OnPropertyChanged();

            if (string.IsNullOrWhiteSpace(value)) return;
            string textoSeguro = value.Replace(",", ".");
            if (decimal.TryParse(textoSeguro, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsed))
            {
                if (parsed < 0) return;

                if (UnidadVenta == UnidadVenta.Unidad && parsed % 1 != 0)
                    _cantidad = Math.Round(parsed, 0);
                else
                    _cantidad = parsed;

                OnPropertyChanged(nameof(Cantidad));
                OnPropertyChanged(nameof(Subtotal));
            }
        }
    }

    // PROPIEDAD MATEMÁTICA (Usada por los botones + y -)
    public decimal Cantidad
    {
        get => _cantidad;
        set
        {
            if (value < 0) return;

            if (UnidadVenta == UnidadVenta.Unidad && value % 1 != 0)
                _cantidad = Math.Round(value, 0);
            else
                _cantidad = value;

            // Sincroniza la caja de texto solo si el valor matemático realmente cambió 
            // (Ej: Cuando apretamos el botón +)
            string textoSeguro = _cantidadTexto.Replace(",", ".");
            decimal.TryParse(textoSeguro, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal actualEnTexto);

            if (_cantidad != actualEnTexto)
            {
                _cantidadTexto = _cantidad.ToString(System.Globalization.CultureInfo.InvariantCulture);
                OnPropertyChanged(nameof(CantidadTexto));
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
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PuedeInteractuar))]
    private bool _procesandoCobro;
    public bool PuedeInteractuar => !ProcesandoCobro;

    // ==========================================
    // NOTIFICACIONES (SNACKBAR)
    // ==========================================
    [ObservableProperty]
    private bool _isSnackbarActive;

    [ObservableProperty]
    private string _mensajeSnackbar = string.Empty;

    private async void MostrarMensaje(string mensaje)
    {
        MensajeSnackbar = mensaje;
        IsSnackbarActive = true;
        await Task.Delay(3500); // Se desvanece a los 3.5 segundos
        IsSnackbarActive = false;
    }

    // ==========================================
    // CONTROL DE PESTAÑAS (SUB-MENÚ)
    // ==========================================
    [ObservableProperty]
    private bool _isNuevaVentaVisible = true;

    [ObservableProperty]
    private bool _isHistorialVisible = false;

    public string FondoBotonNuevaVenta => IsNuevaVentaVisible ? "#3B82F6" : "#E2E8F0";
    public string TextoBotonNuevaVenta => IsNuevaVentaVisible ? "White" : "#475569";
    public string FondoBotonHistorial => IsHistorialVisible ? "#3B82F6" : "#E2E8F0";
    public string TextoBotonHistorial => IsHistorialVisible ? "White" : "#475569";

    [RelayCommand]
    private void MostrarNuevaVenta()
    {
        IsNuevaVentaVisible = true;
        IsHistorialVisible = false;
        NotificarCambiosPestanas();
    }

    [RelayCommand]
    private async Task MostrarHistorialAsync()
    {
        IsNuevaVentaVisible = false;
        IsHistorialVisible = true;
        NotificarCambiosPestanas();
        await CargarHistorialAsync();
    }

    private void NotificarCambiosPestanas()
    {
        OnPropertyChanged(nameof(FondoBotonNuevaVenta));
        OnPropertyChanged(nameof(TextoBotonNuevaVenta));
        OnPropertyChanged(nameof(FondoBotonHistorial));
        OnPropertyChanged(nameof(TextoBotonHistorial));
    }
    // ==========================================

    private readonly IProductoRepository _productoRepository;
    private readonly RegistrarVentaUseCase _registrarVentaUseCase;
    private readonly IVentaRepository _ventaRepository;
    private readonly ISesionUsuario _sesionUsuario;

    private List<Producto> _todosLosProductos = new();

    public ObservableCollection<Producto> ProductosFiltrados { get; } = new();
    public ObservableCollection<DetalleCarritoVenta> Carrito { get; } = new();
    public ObservableCollection<Venta> HistorialVentas { get; } = new();

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
        IVentaRepository ventaRepository,
        ISesionUsuario sesionUsuario)
    {
        _productoRepository = productoRepository;
        _registrarVentaUseCase = registrarVentaUseCase;
        _ventaRepository = ventaRepository;
        _sesionUsuario = sesionUsuario;
        _ = InicializarCatalogosAsync();
    }

    private async Task InicializarCatalogosAsync()
    {
        try
        {
            await CargarCatalogosAsync();
            await CargarHistorialAsync();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "No se pudieron cargar los datos iniciales"); }
    }

    private async Task CargarCatalogosAsync()
    {
        var productos = await _productoRepository.ObtenerActivosAsync();
        _todosLosProductos = productos.ToList();
    }

    private async Task CargarHistorialAsync()
    {
        HistorialVentas.Clear();
        var historial = await _ventaRepository.ObtenerHistorialVentasAsync();
        foreach (var venta in historial)
        {
            HistorialVentas.Add(venta);
        }
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
            var detalle = new DetalleCarritoVenta
            {
                ProductoId = producto.Id,
                CodigoBarras = producto.CodigoBarras ?? "S/C",
                Nombre = producto.Nombre,
                UnidadVenta = producto.UnidadVenta,
                PrecioUnitario = producto.PrecioVenta,
                Cantidad = 1,
                CantidadTexto = "1"
            };
            detalle.PropertyChanged += DetalleCambiado;
            Carrito.Add(detalle);

            // GATILLO DEL SNACKBAR SI ES POR PESO
            if (producto.UnidadVenta == UnidadVenta.Peso)
            {
                MostrarMensaje($"Producto por peso: Ingrese kilos usando el teclado (Ej: 0.250).");
            }
        }
        OnPropertyChanged(nameof(TotalVenta));
    }

    private void DetalleCambiado(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DetalleCarritoVenta.Subtotal))
            OnPropertyChanged(nameof(TotalVenta));
    }

    [RelayCommand]
    private void QuitarDelCarrito(DetalleCarritoVenta detalle)
    {
        if (detalle != null && Carrito.Contains(detalle))
        {
            Carrito.Remove(detalle);
            detalle.PropertyChanged -= DetalleCambiado;
            OnPropertyChanged(nameof(TotalVenta));
        }
    }

    [RelayCommand]
    private void SumarCantidad(DetalleCarritoVenta detalle)
    {
        if (detalle == null) return;
        detalle.Cantidad += 1;
    }

    [RelayCommand]
    private void RestarCantidad(DetalleCarritoVenta detalle)
    {
        if (detalle == null) return;

        if (detalle.Cantidad > 1)
        {
            detalle.Cantidad -= 1;
        }
        else
        {
            QuitarDelCarrito(detalle);
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
        if (ProcesandoCobro || !Carrito.Any()) return;
        ProcesandoCobro = true;
        try
        {
            var usuario = _sesionUsuario.UsuarioActual
                ?? throw new InvalidOperationException("Inicia sesión antes de registrar una venta.");

            var productosTicket = Carrito.Select(x => new DetalleCarritoVenta
            {
                ProductoId = x.ProductoId,
                Nombre = x.Nombre,
                CodigoBarras = x.CodigoBarras,
                UnidadVenta = x.UnidadVenta,
                Cantidad = x.Cantidad,
                PrecioUnitario = x.PrecioUnitario
            }).ToList();

            var total = productosTicket.Sum(x => x.Subtotal);
            decimal subtotalCalc = Math.Round(total / 1.18m, 2);
            decimal igvCalc = total - subtotalCalc;

            var request = new RegistrarVentaRequest
            {
                UsuarioId = usuario.IdUsuario,
                Total = total,
                Subtotal = subtotalCalc,
                IGV = igvCalc,
                MetodoPago = metodoPago,
                Detalles = productosTicket.Select(item => new DetalleVentaRequest
                {
                    ProductoId = item.ProductoId,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario,
                    Subtotal = item.Subtotal
                }).ToList()
            };

            int nuevaVentaId = await _registrarVentaUseCase.ExecuteAsync(request);

            ModalCobroVisible = Visibility.Collapsed;
            foreach (var detalle in Carrito) detalle.PropertyChanged -= DetalleCambiado;
            Carrito.Clear();
            OnPropertyChanged(nameof(TotalVenta));
            TextoBusquedaProducto = "";
            ProductoSeleccionado = null;

            await CargarCatalogosAsync();
            await CargarHistorialAsync();

            try
            {
                var ventanaTicket = new BodegaLuchito.Desktop.Modules.Ventas.Views.TicketVentaWindow(
                    nuevaVentaId, usuario.NombreCompleto, metodoPago, subtotalCalc, igvCalc, total, productosTicket);
                ventanaTicket.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"La venta se registró, pero falló la impresión: {ex.Message}", "Aviso");
            }
        }
        catch (InvalidOperationException ex)
            when (ex.Message.Contains("sesión de caja", StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(ex.Message,"Caja no abierta", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error al procesar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { ProcesandoCobro = false; }
    }
       
    [RelayCommand]
    private void VerDetallesVenta(Venta venta)
    {
        if (venta == null) return;

        try
        {
            var productosTicket = venta.Detalles.Select(d =>
            {
                var prodMemoria = _todosLosProductos.FirstOrDefault(p => p.Id == d.ProductoId);
                return new DetalleCarritoVenta
                {
                    ProductoId = d.ProductoId,
                    CodigoBarras = prodMemoria?.CodigoBarras ?? "S/C",
                    Nombre = prodMemoria?.Nombre ?? "Producto",
                    UnidadVenta = prodMemoria?.UnidadVenta ?? UnidadVenta.Unidad,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario
                };
            }).ToList();

            var ventanaTicket = new BodegaLuchito.Desktop.Modules.Ventas.Views.TicketVentaWindow(
                venta.Id,
                venta.Usuario?.NombreCompleto ?? "Usuario",
                venta.MetodoPago,
                venta.Subtotal,
                venta.IGV,
                venta.Total,
                productosTicket);

            ventanaTicket.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al cargar los detalles del ticket: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
