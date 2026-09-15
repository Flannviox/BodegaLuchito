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
    private readonly IProductoRepository _productoRepository;

    [ObservableProperty] private string _nombre = string.Empty;
    [ObservableProperty] private string _categoria = string.Empty;
    [ObservableProperty] private string? _codigoBarras;
    [ObservableProperty] private decimal _precioVenta;
    [ObservableProperty] private UnidadVenta _unidadVenta = UnidadVenta.Unidad;
    [ObservableProperty] private bool _controlaInventario = true;
    [ObservableProperty] private decimal _stockActual;
    [ObservableProperty] private decimal _stockMinimo;

    // Propiedad para controlar la visibilidad del Modal
    [ObservableProperty] private Visibility _modalVisible = Visibility.Collapsed;

    public IEnumerable<UnidadVenta> UnidadesVenta => Enum.GetValues(typeof(UnidadVenta)).Cast<UnidadVenta>();
    public ObservableCollection<Producto> Productos { get; } = new();

    public RegistrarProductoViewModel(
        RegistrarProductoUseCase registrarUseCase,
        IProductoRepository productoRepository)
    {
        _registrarUseCase = registrarUseCase;
        _productoRepository = productoRepository;

        CargarProductosAsync().ConfigureAwait(false);
    }

    // Comandos para abrir y cerrar el modal
    [RelayCommand]
    private void AbrirModal() => ModalVisible = Visibility.Visible;

    [RelayCommand]
    private void CerrarModal()
    {
        LimpiarFormulario();
        ModalVisible = Visibility.Collapsed;
    }

    [RelayCommand]
    private async Task RegistrarAsync()
    {
        try
        {
            var request = new RegistrarProductoRequest(Nombre, Categoria, CodigoBarras, PrecioVenta, UnidadVenta, ControlaInventario, StockActual, StockMinimo);
            await _registrarUseCase.EjecutarAsync(request);

            MessageBox.Show("Producto registrado con éxito.", "Bodega Luchito", MessageBoxButton.OK, MessageBoxImage.Information);

            await CargarProductosAsync();
            CerrarModal(); // Cierra el modal y limpia el formulario al tener éxito
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error de Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async Task CargarProductosAsync()
    {
        var productosBD = await _productoRepository.ObtenerActivosAsync();
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            Productos.Clear();
            foreach (var p in productosBD)
            {
                Productos.Add(p);
            }
        });
    }

    private void LimpiarFormulario()
    {
        Nombre = string.Empty;
        Categoria = string.Empty;
        CodigoBarras = null;
        PrecioVenta = 0;
        UnidadVenta = UnidadVenta.Unidad;
        ControlaInventario = true;
        StockActual = 0;
        StockMinimo = 0;
    }
}
