using System.Collections.ObjectModel;
using System.Globalization;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Desktop.Common.ViewModels;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Productos.ViewModels;

public partial class NuevoProductoIngresoViewModel(ICategoriaRepository categorias) : ViewModelBase
{
    private Func<RegistrarProductoRequest, decimal, decimal, decimal, Task>? _confirmar;
    public ObservableCollection<Categoria> Categorias { get; } = [];
    public UnidadVenta[] Unidades { get; } = [UnidadVenta.Unidad, UnidadVenta.Peso];
    public string[] ModosCosto { get; } = ["Costo por unidad", "Costo total del lote"];
    [ObservableProperty] private bool _visible;
    [ObservableProperty] private bool _ocupado;
    [ObservableProperty] private bool _esCompra;
    [ObservableProperty] private string _nombre = "";
    [ObservableProperty] private string _codigo = "";
    [ObservableProperty] private Categoria? _categoria;
    [ObservableProperty] private UnidadVenta _unidad = UnidadVenta.Unidad;
    [ObservableProperty] private string _precio = "";
    [ObservableProperty] private string _stockMinimo = "0";
    [ObservableProperty] private string _cantidad = "";
    [ObservableProperty] private string _costo = "";
    [ObservableProperty] private string _modoCosto = "Costo por unidad";
    [ObservableProperty] private string _error = "";
    public bool Disponible => !Ocupado;
    public string Titulo => EsCompra ? "Producto nuevo para esta compra" : "Producto nuevo y saldo inicial";
    public string TextoConfirmar => EsCompra ? "Añadir al abastecimiento" : "Registrar producto y saldo";
    public string Explicacion => EsCompra
        ? "Se guardará al confirmar el abastecimiento. Si lo quitas del detalle, no se creará en el catálogo."
        : "Registra la mercadería que ya tenías. El producto y su cantidad se guardarán juntos, sin pago ni proveedor.";
    public string EtiquetaCantidad => Unidad == UnidadVenta.Peso ? "Cantidad que ingresa (kg)" : "Cantidad que ingresa (unidades)";
    public string EtiquetaPrecio => Unidad == UnidadVenta.Peso ? "Precio de venta por kg (S/)" : "Precio de venta por unidad (S/)";
    public string ResumenCosto
    {
        get
        {
            if (!Leer(Cantidad, out var cantidad) || !Leer(Costo, out var costo) || cantidad <= 0)
                return "El costo de compra es distinto del precio de venta.";
            var total = ModoCosto == ModosCosto[0] ? Math.Round(cantidad * costo, 2, MidpointRounding.AwayFromZero) : costo;
            var unitario = ModoCosto == ModosCosto[0] ? costo : total / cantidad;
            return $"Total de compra: S/ {total:0.00} · Costo por unidad: S/ {unitario:0.0000}";
        }
    }
    partial void OnOcupadoChanged(bool value) => OnPropertyChanged(nameof(Disponible));
    partial void OnUnidadChanged(UnidadVenta value) { OnPropertyChanged(nameof(EtiquetaCantidad)); OnPropertyChanged(nameof(EtiquetaPrecio)); }
    partial void OnCantidadChanged(string value) => OnPropertyChanged(nameof(ResumenCosto));
    partial void OnCostoChanged(string value) => OnPropertyChanged(nameof(ResumenCosto));
    partial void OnModoCostoChanged(string value) => OnPropertyChanged(nameof(ResumenCosto));

    public async Task AbrirAsync(bool compra, Func<RegistrarProductoRequest, decimal, decimal, decimal, Task> confirmar,
        RegistrarProductoRequest? datos = null, decimal? cantidad = null, decimal? total = null)
    {
        _confirmar = confirmar;
        EsCompra = compra;
        OnPropertyChanged(nameof(Titulo)); OnPropertyChanged(nameof(TextoConfirmar)); OnPropertyChanged(nameof(Explicacion));
        Nombre = datos?.Nombre ?? ""; Codigo = datos?.CodigoBarras ?? "";
        Unidad = datos?.UnidadVenta ?? UnidadVenta.Unidad;
        Precio = datos?.PrecioVenta.ToString(CultureInfo.InvariantCulture) ?? "";
        StockMinimo = datos?.StockMinimo.ToString(CultureInfo.InvariantCulture) ?? "0";
        Cantidad = cantidad?.ToString(CultureInfo.InvariantCulture) ?? "";
        Costo = total?.ToString(CultureInfo.InvariantCulture) ?? "";
        ModoCosto = total.HasValue ? ModosCosto[1] : ModosCosto[0];
        Error = ""; Categoria = null; Visible = true; Ocupado = true;
        try
        {
            Categorias.Clear();
            foreach (var categoria in await categorias.ObtenerActivasAsync()) Categorias.Add(categoria);
            Categoria = Categorias.FirstOrDefault(x => x.Id == datos?.CategoriaId);
            if (Categorias.Count == 0) Error = "Primero registra una categoría desde Productos.";
        }
        catch (Exception ex) { Error = "No se pudieron cargar las categorías: " + ex.Message; }
        finally { Ocupado = false; }
    }

    private static bool Leer(string texto, out decimal valor) => decimal.TryParse(texto.Trim().Replace(',', '.'),
        NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out valor);

    [RelayCommand] private void Cancelar() { if (!Ocupado) Visible = false; }
    [RelayCommand]
    private async Task ConfirmarAsync()
    {
        if (Ocupado || _confirmar is null) return;
        Error = "";
        try
        {
            if (!Leer(Precio, out var precio) || !Leer(StockMinimo, out var minimo))
                throw new ArgumentException("Revisa el precio de venta y el stock mínimo; puedes usar punto o coma decimal.");
            if (decimal.Round(precio, 2) != precio || decimal.Round(minimo, 3) != minimo)
                throw new ArgumentException("Usa hasta dos decimales en el precio y tres en el stock mínimo.");
            var datos = new RegistrarProductoRequest(Nombre, Categoria?.Id ?? 0, Codigo, precio, Unidad, true, 0, minimo);
            PrepararProductoNuevo.Crear(datos);
            if (!Leer(Cantidad, out var cantidad) || cantidad <= 0 || cantidad > 999999999m || decimal.Round(cantidad, 3) != cantidad)
                throw new ArgumentException("Ingresa una cantidad mayor que cero, con hasta tres decimales.");
            if (Unidad == UnidadVenta.Unidad && cantidad % 1 != 0)
                throw new ArgumentException("Los productos por unidad requieren cantidades enteras.");
            decimal unitario = 0, total = 0;
            if (EsCompra)
            {
                if (!Leer(Costo, out var costo) || costo <= 0)
                    throw new ArgumentException("Ingresa un costo de compra mayor que cero.");
                if (decimal.Round(costo, ModoCosto == ModosCosto[0] ? 4 : 2) != costo)
                    throw new ArgumentException("Usa hasta cuatro decimales para costo por unidad o dos para el total del lote.");
                total = ModoCosto == ModosCosto[0] ? Math.Round(cantidad * costo, 2, MidpointRounding.AwayFromZero) : costo;
                unitario = ModoCosto == ModosCosto[0] ? costo : Math.Round(total / cantidad, 4, MidpointRounding.AwayFromZero);
                if (total <= 0 || unitario <= 0) throw new ArgumentException("El costo resultante es demasiado pequeño.");
            }
            Ocupado = true;
            await _confirmar(datos, cantidad, unitario, total);
            Visible = false;
        }
        catch (Exception ex) { Error = ex.Message; }
        finally { Ocupado = false; }
    }
}
