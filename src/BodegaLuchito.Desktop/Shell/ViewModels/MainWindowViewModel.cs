using System.Collections.ObjectModel;
using BodegaLuchito.Application.Common.Session;
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
    private readonly ISesionUsuario _sesionUsuario;

    public MainWindowViewModel(
        INavigationService navigationService,
        ISesionUsuario sesionUsuario)
    {
        _navigationService = navigationService;
        _sesionUsuario = sesionUsuario;

        _navigationService.CurrentViewModelChanged +=
            OnCurrentViewModelChanged;

        _sesionUsuario.SesionCambiada +=
            OnSesionCambiada;

        MenuItems =
        [
            new NavigationItemViewModel(
                "Inicio",
                IrAInicioCommand),

            new NavigationItemViewModel(
                "Productos",
                IrAProductosCommand),

            new NavigationItemViewModel(
                "Ventas",
                IrAVentasCommand)
        ];

        _navigationService.NavigateTo<InicioViewModel>();
    }

    public string Titulo =>
        "Bodega Luchito";

    public ViewModelBase? CurrentViewModel =>
        _navigationService.CurrentViewModel;

    public ObservableCollection<NavigationItemViewModel> MenuItems
    {
        get;
    }

    public string NombreUsuario =>
        _sesionUsuario.UsuarioActual?.NombreUsuario
        ?? "Sin sesion";

    public string NombreRol =>
        _sesionUsuario.UsuarioActual?.NombreRol
        ?? string.Empty;

    public bool EstaAutenticado =>
        _sesionUsuario.EstaAutenticado;

    private void OnCurrentViewModelChanged()
    {
        OnPropertyChanged(nameof(CurrentViewModel));
    }

    private void OnSesionCambiada()
    {
        OnPropertyChanged(nameof(NombreUsuario));
        OnPropertyChanged(nameof(NombreRol));
        OnPropertyChanged(nameof(EstaAutenticado));

        CerrarSesionCommand.NotifyCanExecuteChanged();
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

    private bool PuedeCerrarSesion()
    {
        return _sesionUsuario.EstaAutenticado;
    }

    [RelayCommand(CanExecute = nameof(PuedeCerrarSesion))]
    private void CerrarSesion()
    {
        _sesionUsuario.CerrarSesion();

        _navigationService.NavigateTo<InicioViewModel>();
    }
}
