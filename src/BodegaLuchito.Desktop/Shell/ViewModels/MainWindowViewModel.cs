using BodegaLuchito.Desktop.Common.ViewModels;
using BodegaLuchito.Desktop.Modules.Inicio.ViewModels;
using BodegaLuchito.Desktop.Modules.Productos.ViewModels;
using BodegaLuchito.Desktop.Modules.Ventas.ViewModels;
using BodegaLuchito.Desktop.Navigation;
using CommunityToolkit.Mvvm.Input;

namespace BodegaLuchito.Desktop.Shell.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly INavigationService _navigationService;

    public MainWindowViewModel(
        INavigationService navigationService)
    {
        _navigationService = navigationService;

        _navigationService.CurrentViewModelChanged +=
            OnCurrentViewModelChanged;

        _navigationService.NavigateTo<InicioViewModel>();
    }

    public string Titulo =>
        "Bodega Luchito";

    public ViewModelBase? CurrentViewModel =>
        _navigationService.CurrentViewModel;

    private void OnCurrentViewModelChanged()
    {
        OnPropertyChanged(nameof(CurrentViewModel));
    }

    [RelayCommand]
    private void IrAInicio()
    {
        _navigationService.NavigateTo<InicioViewModel>();
    }

    [RelayCommand]
    private void IrAProductos()
    {
        _navigationService.NavigateTo<ProductosViewModel>();
    }

    [RelayCommand]
    private void IrAVentas()
    {
        _navigationService.NavigateTo<VentasViewModel>();
    }
}
