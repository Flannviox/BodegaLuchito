using System.Collections.ObjectModel;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Desktop.Common.ViewModels;
using BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;
using BodegaLuchito.Desktop.Modules.Inicio.ViewModels;
using BodegaLuchito.Desktop.Modules.Inventario.ViewModels;
using BodegaLuchito.Desktop.Modules.Productos.ViewModels;
using BodegaLuchito.Desktop.Modules.Proveedores.ViewModels;
using BodegaLuchito.Desktop.Modules.Caja.ViewModels;
using BodegaLuchito.Desktop.Modules.Ventas.ViewModels;
using BodegaLuchito.Desktop.Modules.Abastecimiento.ViewModels;
using BodegaLuchito.Desktop.Navigation;
using CommunityToolkit.Mvvm.Input;
using BodegaLuchito.Desktop.Modules.BI.ViewModels;

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

        ConstruirMenu();

        _navigationService.NavigateTo<InicioViewModel>();
    }

    public string TituloVentana => "Bodega Luchito";

    public string TituloPagina => CurrentViewModel switch
    {
        InicioViewModel => "Inicio",
        ProductosViewModel => "Gestión de productos",
        InventarioViewModel => "Inventario",
        PrediccionDemandaViewModel => "Predicción de demanda",
        ProveedoresViewModel => "Proveedores",
        AbastecimientoViewModel => "Abastecimiento",
        UsuariosViewModel => "Usuarios",
        VentasViewModel => "Ventas",
        CajaViewModel => "Caja",
        _ => "Bodega Luchito"
    };

    public ViewModelBase? CurrentViewModel =>
        _navigationService.CurrentViewModel;

    public ObservableCollection<NavigationItemViewModel> MenuItems { get; }
        = new();

    public string NombreUsuario =>
        _sesionUsuario.UsuarioActual?.NombreUsuario
        ?? "Sin sesion";

    public string NombreRol =>
        _sesionUsuario.UsuarioActual?.NombreRol
        ?? string.Empty;

    public bool EstaAutenticado =>
        _sesionUsuario.EstaAutenticado;

    public string InicialesUsuario => string.IsNullOrWhiteSpace(NombreCompleto) ? "BL"
        : string.Concat(NombreCompleto.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2).Select(parte => System.Globalization.StringInfo.GetNextTextElement(parte))).ToUpperInvariant();

    private void ConstruirMenu()
    {
        MenuItems.Clear();
        AgregarDestino<InicioViewModel>("Inicio", IrAInicioCommand, IconosNavegacion.Inicio);

        if (_sesionUsuario.UsuarioActual?.EsAdministradora == true)
        {
            AgregarDestino<ProductosViewModel>("Productos", IrAProductosCommand, IconosNavegacion.Productos);
            AgregarDestino<InventarioViewModel>("Inventario", IrAInventarioCommand, IconosNavegacion.Inventario);
            AgregarDestino<PrediccionDemandaViewModel>("Predicción de demanda", IrAPrediccionCommand, IconosNavegacion.Prediccion);
            AgregarDestino<ProveedoresViewModel>("Proveedores", IrAProveedoresCommand, IconosNavegacion.Proveedores);
            AgregarDestino<AbastecimientoViewModel>("Abastecimiento", IrAAbastecimientoCommand, IconosNavegacion.Abastecimiento);
            AgregarDestino<UsuariosViewModel>("Usuarios", IrAUsuariosCommand, IconosNavegacion.Usuarios);
        }

        AgregarDestino<VentasViewModel>("Ventas", IrAVentasCommand, IconosNavegacion.Ventas);
        AgregarDestino<CajaViewModel>("Caja", IrACajaCommand, IconosNavegacion.Caja);
        ActualizarSeleccion();
    }

    private void AgregarDestino<T>(string titulo, System.Windows.Input.ICommand command, string icono)
        where T : ViewModelBase => MenuItems.Add(new NavigationItemViewModel(titulo, command, typeof(T), icono));

    private void ActualizarSeleccion()
    {
        foreach (var item in MenuItems)
            item.EstaSeleccionado = CurrentViewModel is not null && item.TipoDestino.IsInstanceOfType(CurrentViewModel);
    }

    private void OnCurrentViewModelChanged()
    {
        OnPropertyChanged(nameof(CurrentViewModel));
        OnPropertyChanged(nameof(TituloPagina));
        ActualizarSeleccion();
    }

    private void OnSesionCambiada()
    {
        OnPropertyChanged(nameof(NombreUsuario));
        OnPropertyChanged(nameof(NombreRol));
        OnPropertyChanged(nameof(InicialesUsuario));
        OnPropertyChanged(nameof(EstaAutenticado));
        OnPropertyChanged(nameof(NombreCompleto));

        ConstruirMenu();

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
    private void IrAInventario()
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora == true)
            _navigationService.NavigateTo<InventarioViewModel>();
    }

    [RelayCommand]
    private void IrAPrediccion()
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora == true)
            _navigationService.NavigateTo<PrediccionDemandaViewModel>();
    }

    [RelayCommand]
    private void IrAProveedores()
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora != true)
        {
            return;
        }

        _navigationService.NavigateTo<ProveedoresViewModel>();
    }

    [RelayCommand]
    private void IrAAbastecimiento()
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora != true)
        {
            return;
        }

        _navigationService.NavigateTo<AbastecimientoViewModel>();
    }

    [RelayCommand]
    private void IrAUsuarios()
    {
        if (_sesionUsuario.UsuarioActual?.EsAdministradora != true)
        {
            return;
        }

        _navigationService.NavigateTo<UsuariosViewModel>();
    }

    [RelayCommand]
    private void IrAVentas()
    {
        _navigationService.NavigateTo<VentasViewModel>();
    }

    [RelayCommand]
    private void IrACaja()
    {
        if (!_sesionUsuario.EstaAutenticado)
        {
            return;
        }

        _navigationService.NavigateTo<CajaViewModel>();
    }

    private bool PuedeCerrarSesion()
    {
        return _sesionUsuario.EstaAutenticado;
    }

    [RelayCommand(CanExecute = nameof(PuedeCerrarSesion))]
    private void CerrarSesion()
    {
        _sesionUsuario.CerrarSesion();
    }
    public void InicializarParaSesionActual()
    {
        ConstruirMenu();

        OnPropertyChanged(nameof(NombreUsuario));
        OnPropertyChanged(nameof(NombreRol));
        OnPropertyChanged(nameof(InicialesUsuario));
        OnPropertyChanged(nameof(EstaAutenticado));

        OnPropertyChanged(nameof(NombreCompleto));
        _navigationService.NavigateTo<InicioViewModel>();
    }
    public string NombreCompleto =>
    _sesionUsuario.UsuarioActual?.NombreCompleto
    ?? string.Empty;
}
