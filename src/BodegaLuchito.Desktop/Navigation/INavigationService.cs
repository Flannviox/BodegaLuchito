using BodegaLuchito.Desktop.Common.ViewModels;

namespace BodegaLuchito.Desktop.Navigation;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }

    event Action? CurrentViewModelChanged;

    void NavigateTo<TViewModel>()
        where TViewModel : ViewModelBase;
}
