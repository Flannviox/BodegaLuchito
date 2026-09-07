using System.Windows.Input;

namespace BodegaLuchito.Desktop.Shell.ViewModels;

public sealed class NavigationItemViewModel
{
    public NavigationItemViewModel(
        string titulo,
        ICommand command)
    {
        Titulo = titulo;
        Command = command;
    }

    public string Titulo { get; }

    public ICommand Command { get; }
}
