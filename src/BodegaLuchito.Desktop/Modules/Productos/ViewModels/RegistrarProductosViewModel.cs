using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using BodegaLuchito.Application.Productos.DTOs;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Domain.Productos.Entities;
using BodegaLuchito.Domain.Productos.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Modules.Productos.ViewModels;

public partial class RegistrarProductoViewModel : ObservableObject
{
    private readonly RegistrarProductoUseCase _registrarUseCase;
    private readonly IProductoRepository _productoRepository;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _categoria = string.Empty;
    [ObservableProperty] private string? _codigoBarras;
    [ObservableProperty] private decimal _precioVenta;
    [ObservableProperty] private UnidadVenta _unidadVenta = UnidadVenta.Unidad;
    [ObservableProperty] private bool _controlaInventario;
    [ObservableProperty] private decimal _stockActual;
    [ObservableProperty] private decimal _stockMinimo;

    [ObservableProperty] private ObservableCollection<Producto> _productosRegistrados = new();

    public IEnumerable<UnidadVenta> UnidadesDeVenta => Enum.GetValues<UnidadVenta>();

    public RegistrarProductoViewModel(
        RegistrarProductoUseCase registrarUseCase,
        IProductoRepository productoRepository)
    {
        _registrarUseCase = registrarUseCase;
        _productoRepository = productoRepository;
        CargarProductosCommand.ExecuteAsync(null);
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        try
        {
            var request = new RegistrarProductoRequest(
                Nombre, Categoria, CodigoBarras, PrecioVenta,
                UnidadVenta, ControlaInventario, StockActual, StockMinimo);

            await _registrarUseCase.EjecutarAsync(request);

            MessageBox.Show("Producto registrado con éxito.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);

            LimpiarFormulario();
            await CargarProductosAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task CargarProductosAsync()
    {
        var productos = await _productoRepository.ObtenerActivosAsync();
        ProductosRegistrados.Clear();
        foreach (var p in productos)
        {
            ProductosRegistrados.Add(p);
        }
    }

    private void LimpiarFormulario()
    {
        Nombre = string.Empty;
        Categoria = string.Empty;
        CodigoBarras = null;
        PrecioVenta = 0;
        UnidadVenta = UnidadVenta.Unidad;
        ControlaInventario = false;
        StockActual = 0;
        StockMinimo = 0;
    }
}
