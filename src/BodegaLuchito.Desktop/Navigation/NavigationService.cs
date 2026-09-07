using BodegaLuchito.Desktop.Common.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BodegaLuchito.Desktop.Navigation;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ViewModelBase? CurrentViewModel { get; private set; }

    public event Action? CurrentViewModelChanged;

    public void NavigateTo<TViewModel>()
        where TViewModel : ViewModelBase
    {
        CurrentViewModel =
            _serviceProvider.GetRequiredService<TViewModel>();

        CurrentViewModelChanged?.Invoke();
    }
}
