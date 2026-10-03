using System.Collections.ObjectModel;
using System.Globalization;
using BodegaLuchito.Application.Inventario.DTOs;
using BodegaLuchito.Application.Inventario.UseCases;
using BodegaLuchito.Desktop.Common.ViewModels;
using BodegaLuchito.Domain.Inventario.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Desktop.Modules.Productos.ViewModels;

namespace BodegaLuchito.Desktop.Modules.Inventario.ViewModels;

public partial class InventarioViewModel : ViewModelBase
{
    private readonly GestionarInventarioUseCase _gestion;
    private IReadOnlyList<ExistenciaInventario> _existencias = [];
    private IReadOnlyList<MovimientoInventarioDetalle> _historial = [];
    private TipoMovimientoInventario _operacion;
    private int _avisoVersion;

    public NuevoProductoIngresoViewModel AltaProducto { get; }

    public InventarioViewModel(GestionarInventarioUseCase gestion, ICategoriaRepository categorias)
    {
        _gestion = gestion;
        AltaProducto = new NuevoProductoIngresoViewModel(categorias);
        _ = ActualizarAsync();
    }

    public ObservableCollection<ExistenciaInventario> Existencias { get; } = [];
    public ObservableCollection<MovimientoInventarioDetalle> Movimientos { get; } = [];
    public ObservableCollection<ExistenciaInventario> ProductosOperacion { get; } = [];
    public ObservableCollection<string> Categorias { get; } = ["Todas"];
    public string[] Estados { get; } = ["Activos", "Todos", "Stock bajo", "Agotados", "Desactivados"];
    public string[] TiposMovimiento { get; } = ["Todos", "Abastecimiento", "Saldo inicial", "Ajuste por conteo", "Pérdida o daño", "Venta"];

    [ObservableProperty] private string _busqueda = "";
    [ObservableProperty] private string _categoria = "Todas";
    [ObservableProperty] private string _estado = "Activos";
    [ObservableProperty] private string _busquedaMovimiento = "";
    [ObservableProperty] private string _tipoMovimiento = "Todos";
    [ObservableProperty] private DateTime? _desde;
    [ObservableProperty] private DateTime? _hasta;
    [ObservableProperty] private ExistenciaInventario? _seleccionado;
    [ObservableProperty] private ExistenciaInventario? _productoOperacion;
    [ObservableProperty] private MovimientoInventarioDetalle? _movimientoSeleccionado;
    [ObservableProperty] private string _cantidadTexto = "";
    [ObservableProperty] private string _motivo = "";
    [ObservableProperty] private bool _dialogoVisible;
    [ObservableProperty] private bool _ocupado;
    [ObservableProperty] private string _error = "";
    [ObservableProperty] private string _errorFormulario = "";
    [ObservableProperty] private string _aviso = "";
    [ObservableProperty] private bool _avisoVisible;
    [ObservableProperty] private int _pestana;
    [ObservableProperty] private string _tituloOperacion = "";
    [ObservableProperty] private string _explicacionOperacion = "";
    [ObservableProperty] private string _etiquetaCantidad = "";

    public bool Disponible => !Ocupado;
    public int TotalActivos => _existencias.Count(x => x.Activo);
    public int StockBajo => _existencias.Count(x => x.Activo && x.StockMinimo > 0 && x.StockActual <= x.StockMinimo);
    public int Agotados => _existencias.Count(x => x.Activo && x.StockActual == 0);
    public int PendientesInicial => _existencias.Count(x => x.PuedeRegistrarSaldoInicial);
    public string ConteoExistencias => $"{Existencias.Count} productos encontrados";
    public string ConteoMovimientos => $"{Movimientos.Count} movimientos encontrados";
    public string IndicacionUnidad => ProductoOperacion is null ? "Selecciona un producto."
        : $"Stock actual: {ProductoOperacion.StockActual:0.###} {ProductoOperacion.Unidad} · "
        + (ProductoOperacion.Unidad == "kg" ? "Admite hasta tres decimales." : "Ingresa unidades enteras.");
    public string ResumenOperacion
    {
        get
        {
            if (ProductoOperacion is null || !LeerCantidad(out var cantidad))
                return "Completa los datos para revisar el stock resultante.";
            var posterior = _operacion == TipoMovimientoInventario.Merma
                ? ProductoOperacion.StockActual - cantidad : cantidad;
            return $"Stock: {ProductoOperacion.StockActual:0.###} → {posterior:0.###} {ProductoOperacion.Unidad}";
        }
    }

    partial void OnBusquedaChanged(string value) => FiltrarExistencias();
    partial void OnCategoriaChanged(string value) => FiltrarExistencias();
    partial void OnEstadoChanged(string value) => FiltrarExistencias();
    partial void OnBusquedaMovimientoChanged(string value) => FiltrarMovimientos();
    partial void OnTipoMovimientoChanged(string value) => FiltrarMovimientos();
    partial void OnDesdeChanged(DateTime? value) => FiltrarMovimientos();
    partial void OnHastaChanged(DateTime? value) => FiltrarMovimientos();
    partial void OnOcupadoChanged(bool value) => OnPropertyChanged(nameof(Disponible));
    partial void OnProductoOperacionChanged(ExistenciaInventario? value)
    {
        CantidadTexto = "";
        OnPropertyChanged(nameof(IndicacionUnidad));
        OnPropertyChanged(nameof(ResumenOperacion));
    }
    partial void OnCantidadTextoChanged(string value) => OnPropertyChanged(nameof(ResumenOperacion));

    private bool LeerCantidad(out decimal cantidad) =>
        decimal.TryParse(CantidadTexto.Trim().Replace(',', '.'),
            NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out cantidad);

    private async Task CargarAsync()
    {
        _existencias = await _gestion.ConsultarExistenciasAsync();
        _historial = await _gestion.ConsultarHistorialAsync();
        var categoria = Categoria;
        Categorias.Clear();
        Categorias.Add("Todas");
        foreach (var nombre in _existencias.Select(x => x.Categoria).Distinct().Order())
            Categorias.Add(nombre);
        Categoria = Categorias.Contains(categoria) ? categoria : "Todas";
        FiltrarExistencias();
        FiltrarMovimientos();
        OnPropertyChanged(nameof(TotalActivos));
        OnPropertyChanged(nameof(StockBajo));
        OnPropertyChanged(nameof(Agotados));
        OnPropertyChanged(nameof(PendientesInicial));
    }

    [RelayCommand]
    private async Task ActualizarAsync()
    {
        if (Ocupado) return;
        Ocupado = true;
        Error = "";
        try { await CargarAsync(); }
        catch (Exception ex) { Error = ex.Message; }
        finally { Ocupado = false; }
    }

    private void FiltrarExistencias()
    {
        var id = Seleccionado?.ProductoId;
        var texto = Busqueda.Trim();
        var datos = _existencias.Where(x =>
            (texto.Length == 0 || x.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || (x.Codigo?.Contains(texto, StringComparison.OrdinalIgnoreCase) ?? false)) &&
            (Categoria == "Todas" || x.Categoria == Categoria) &&
            (Estado switch
            {
                "Activos" => x.Activo,
                "Desactivados" => !x.Activo,
                "Stock bajo" => x.Activo && x.StockMinimo > 0 && x.StockActual <= x.StockMinimo,
                "Agotados" => x.Activo && x.StockActual == 0,
                _ => true
            }));
        Existencias.Clear();
        foreach (var item in datos) Existencias.Add(item);
        Seleccionado = Existencias.FirstOrDefault(x => x.ProductoId == id);
        OnPropertyChanged(nameof(ConteoExistencias));
    }

    private void FiltrarMovimientos()
    {
        var id = MovimientoSeleccionado?.Id;
        var texto = BusquedaMovimiento.Trim();
        Movimientos.Clear();
        foreach (var item in _historial.Where(x =>
            (texto.Length == 0 || x.Producto.Contains(texto, StringComparison.OrdinalIgnoreCase)
                || x.Motivo.Contains(texto, StringComparison.OrdinalIgnoreCase)) &&
            (TipoMovimiento == "Todos" || x.TipoDescripcion == TipoMovimiento) &&
            (!Desde.HasValue || x.Fecha.Date >= Desde.Value.Date) &&
            (!Hasta.HasValue || x.Fecha.Date <= Hasta.Value.Date)))
            Movimientos.Add(item);
        MovimientoSeleccionado = Movimientos.FirstOrDefault(x => x.Id == id) ?? Movimientos.FirstOrDefault();
        OnPropertyChanged(nameof(ConteoMovimientos));
    }

    [RelayCommand]
    private void Limpiar()
    {
        Busqueda = ""; Categoria = "Todas"; Estado = "Activos";
        BusquedaMovimiento = ""; TipoMovimiento = "Todos"; Desde = null; Hasta = null;
    }

    [RelayCommand] private void AbrirInicial() => Abrir(TipoMovimientoInventario.SaldoInicial);
    [RelayCommand]
    private async Task AbrirProductoNuevoAsync()
    {
        if (Ocupado) return;
        DialogoVisible = false;
        Error = "";
        await AltaProducto.AbrirAsync(false, async (producto, cantidad, _, _) =>
        {
            await _gestion.RegistrarAsync(new(0, TipoMovimientoInventario.SaldoInicial, cantidad, 0,
                "Mercadería existente al iniciar el sistema.", producto));
            var version = ++_avisoVersion;
            Aviso = "Producto y saldo inicial registrados.";
            AvisoVisible = true;
            _ = OcultarAvisoAsync(version);
            try { await CargarAsync(); }
            catch (Exception ex) { Error = "Se guardó el producto y su saldo. No se pudo recargar: " + ex.Message; }
        });
    }
    [RelayCommand] private void AbrirConteo() => Abrir(TipoMovimientoInventario.AjusteConteo);
    [RelayCommand] private void AbrirMerma() => Abrir(TipoMovimientoInventario.Merma);

    private void Abrir(TipoMovimientoInventario tipo)
    {
        if (Ocupado) return;
        _operacion = tipo;
        ProductosOperacion.Clear();
        foreach (var item in _existencias.Where(x => x.Activo &&
            (tipo != TipoMovimientoInventario.SaldoInicial || x.PuedeRegistrarSaldoInicial)))
            ProductosOperacion.Add(item);
        if (ProductosOperacion.Count == 0)
        {
            Error = tipo == TipoMovimientoInventario.SaldoInicial
                ? "No hay productos pendientes de saldo inicial. Usa «Producto y saldo inicial» para registrar uno nuevo aquí mismo."
                : "No hay productos activos. Registra o reactiva un producto primero.";
            return;
        }
        Error = ""; ErrorFormulario = "";
        TituloOperacion = tipo switch
        {
            TipoMovimientoInventario.SaldoInicial => "Registrar inventario inicial",
            TipoMovimientoInventario.AjusteConteo => "Ajustar por conteo",
            _ => "Registrar pérdida o daño"
        };
        ExplicacionOperacion = tipo switch
        {
            TipoMovimientoInventario.SaldoInicial => "Mercadería que ya existía antes de usar el sistema. Disponible una sola vez por producto, antes de sus movimientos.",
            TipoMovimientoInventario.AjusteConteo => "Cuenta la mercadería e ingresa la cantidad total que realmente hay. Se registrará la diferencia.",
            _ => "Ingresa la cantidad que se retira por daño, vencimiento o pérdida."
        };
        EtiquetaCantidad = tipo == TipoMovimientoInventario.AjusteConteo ? "Cantidad total contada"
            : tipo == TipoMovimientoInventario.Merma ? "Cantidad que se retira" : "Cantidad inicial";
        ProductoOperacion = ProductosOperacion.FirstOrDefault(x => x.ProductoId == Seleccionado?.ProductoId)
            ?? ProductosOperacion.First();
        CantidadTexto = "";
        Motivo = tipo == TipoMovimientoInventario.SaldoInicial ? "Mercadería existente al iniciar el sistema." : "";
        OnPropertyChanged(nameof(ResumenOperacion));
        DialogoVisible = true;
    }

    [RelayCommand] private void Cancelar() { if (!Ocupado) DialogoVisible = false; }

    [RelayCommand]
    private async Task GuardarAsync()
    {
        if (Ocupado || !DialogoVisible || ProductoOperacion is null) return;
        if (!LeerCantidad(out var cantidad))
        {
            ErrorFormulario = "Ingresa una cantidad válida; puedes usar punto o coma para los decimales.";
            return;
        }
        Ocupado = true;
        ErrorFormulario = "";
        try
        {
            await _gestion.RegistrarAsync(new(ProductoOperacion.ProductoId, _operacion,
                cantidad, ProductoOperacion.StockActual, Motivo));
            DialogoVisible = false;
            var version = ++_avisoVersion;
            Aviso = "Movimiento registrado. El stock y el historial se actualizaron.";
            AvisoVisible = true;
            _ = OcultarAvisoAsync(version);
            try { await CargarAsync(); }
            catch (Exception ex) { Error = "El movimiento se guardó. No se pudo actualizar la vista: " + ex.Message; }
        }
        catch (Exception ex) { ErrorFormulario = ex.Message; }
        finally { Ocupado = false; }
    }

    private async Task OcultarAvisoAsync(int version)
    {
        await Task.Delay(4500);
        if (version == _avisoVersion) AvisoVisible = false;
    }
}
