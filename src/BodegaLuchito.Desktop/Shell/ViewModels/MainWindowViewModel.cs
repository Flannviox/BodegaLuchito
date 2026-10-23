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
using BodegaLuchito.Desktop.Modules.Reportes.ViewModels;

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

        IrAInicio();
    }

    public string TituloVentana => "Bodega Luchito";

    public string TituloPagina => CurrentViewModel switch
    {
        DashboardViewModel => "Inicio",
        ProductosViewModel => "Gestión de productos",
        InventarioViewModel => "Inventario",
        PrediccionDemandaViewModel => "Predicción de demanda",
        ProveedoresViewModel => "Proveedores",
        AbastecimientoViewModel => "Abastecimiento",
        UsuariosViewModel => "Usuarios",
        ReportesViewModel => "Reportes comerciales",
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
        if (!PuedeOperar()) return;
        AgregarDestino<DashboardViewModel>("Inicio", IrAInicioCommand, IconosNavegacion.Inicio);
        AgregarDestino<ProductosViewModel>("Productos", IrAProductosCommand, IconosNavegacion.Productos);
        AgregarDestino<InventarioViewModel>("Inventario", IrAInventarioCommand, IconosNavegacion.Inventario);
        if (PuedeAdministrar())
            AgregarDestino<PrediccionDemandaViewModel>("Predicción de demanda", IrAPrediccionCommand, IconosNavegacion.Prediccion);
        AgregarDestino<ProveedoresViewModel>("Proveedores", IrAProveedoresCommand, IconosNavegacion.Proveedores);
        AgregarDestino<AbastecimientoViewModel>("Abastecimiento", IrAAbastecimientoCommand, IconosNavegacion.Abastecimiento);
        if (PuedeAdministrar())
        {
            AgregarDestino<UsuariosViewModel>("Usuarios", IrAUsuariosCommand, IconosNavegacion.Usuarios);
            AgregarDestino<ReportesViewModel>("Reportes", IrAReportesCommand, IconosNavegacion.Reportes);
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

        foreach (var command in new IRelayCommand[] { IrAInicioCommand, IrAProductosCommand, IrAInventarioCommand, IrAPrediccionCommand, IrAProveedoresCommand, IrAAbastecimientoCommand, IrAUsuariosCommand, IrAVentasCommand, IrACajaCommand, IrAReportesCommand })
            command.NotifyCanExecuteChanged();
        CerrarSesionCommand.NotifyCanExecuteChanged();
        if (PuedeOperar() && CurrentViewModel is null) IrAInicio();
    }

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private void IrAInicio()
    {
        if (PuedeOperar()) _navigationService.NavigateTo<DashboardViewModel>();
    }

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private void IrAProductos()
    {
        if (PuedeOperar()) _navigationService.NavigateTo<ProductosViewModel>();
    }

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private void IrAInventario()
    {
        if (PuedeOperar()) _navigationService.NavigateTo<InventarioViewModel>();
    }

    [RelayCommand(CanExecute = nameof(PuedeAdministrar))]
    private void IrAPrediccion()
    {
        if (PuedeAdministrar()) _navigationService.NavigateTo<PrediccionDemandaViewModel>();
    }

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private void IrAProveedores()
    {
        if (PuedeOperar()) _navigationService.NavigateTo<ProveedoresViewModel>();
    }

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private void IrAAbastecimiento()
    {
        if (PuedeOperar()) _navigationService.NavigateTo<AbastecimientoViewModel>();
    }

    [RelayCommand(CanExecute = nameof(PuedeAdministrar))]
    private void IrAUsuarios()
    {
        if (PuedeAdministrar()) _navigationService.NavigateTo<UsuariosViewModel>();
    }

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private void IrAVentas()
    {
        if (PuedeOperar()) _navigationService.NavigateTo<VentasViewModel>();
    }

    [RelayCommand(CanExecute = nameof(PuedeOperar))]
    private void IrACaja()
    {
        if (PuedeOperar()) _navigationService.NavigateTo<CajaViewModel>();
    }

    private bool PuedeOperar() => _sesionUsuario.UsuarioActual?.PuedeOperar == true;
    private bool PuedeAdministrar() => PuedeOperar() && _sesionUsuario.UsuarioActual!.EsAdministradora;

    private bool PuedeCerrarSesion()
    {
        return _sesionUsuario.EstaAutenticado;
    }

    [RelayCommand(CanExecute = nameof(PuedeCerrarSesion))]
    private void CerrarSesion()
    {
        _sesionUsuario.CerrarSesion();
    }
    [RelayCommand(CanExecute = nameof(PuedeAdministrar))]
    private void IrAReportes()
    {
        if (PuedeAdministrar()) _navigationService.NavigateTo<ReportesViewModel>();
    }
    public void InicializarParaSesionActual()
    {
        ConstruirMenu();

        OnPropertyChanged(nameof(NombreUsuario));
        OnPropertyChanged(nameof(NombreRol));
        OnPropertyChanged(nameof(InicialesUsuario));
        OnPropertyChanged(nameof(EstaAutenticado));

        OnPropertyChanged(nameof(NombreCompleto));
        _navigationService.NavigateTo<DashboardViewModel>();
    }

    public string NombreCompleto =>
    _sesionUsuario.UsuarioActual?.NombreCompleto
    ?? string.Empty;
}
