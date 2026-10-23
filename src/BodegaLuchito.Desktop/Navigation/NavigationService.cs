using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Desktop.Common.ViewModels;
using BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;
using BodegaLuchito.Desktop.Modules.BI.ViewModels;
using BodegaLuchito.Desktop.Modules.Reportes.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BodegaLuchito.Desktop.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISesionUsuario _sesionUsuario;
    public NavigationService(IServiceProvider serviceProvider, ISesionUsuario sesionUsuario)
    {
        _serviceProvider = serviceProvider;
        _sesionUsuario = sesionUsuario;
        _sesionUsuario.SesionCambiada += () =>
        {
            if (CurrentViewModel is not null && !PuedeNavegar(CurrentViewModel.GetType()))
            {
                CurrentViewModel = null;
                CurrentViewModelChanged?.Invoke();
            }
        };
    }
    public ViewModelBase? CurrentViewModel { get; private set; }
    public event Action? CurrentViewModelChanged;

    private bool PuedeNavegar(Type destino)
    {
        var usuario = _sesionUsuario.UsuarioActual;
        if (usuario?.PuedeOperar != true) return false;
        var restringido = typeof(UsuariosViewModel).IsAssignableFrom(destino) ||
            typeof(ReportesViewModel).IsAssignableFrom(destino) ||
            typeof(PrediccionDemandaViewModel).IsAssignableFrom(destino);
        return !restringido || usuario.EsAdministradora;
    }
    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        // Validar antes de resolver el ViewModel: algunos cargan datos en su constructor.
        if (!PuedeNavegar(typeof(TViewModel))) return;
        CurrentViewModel = _serviceProvider.GetRequiredService<TViewModel>();
        CurrentViewModelChanged?.Invoke();
    }
}
