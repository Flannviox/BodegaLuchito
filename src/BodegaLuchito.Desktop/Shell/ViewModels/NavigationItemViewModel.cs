using System.Windows.Input;
using BodegaLuchito.Desktop.Common.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BodegaLuchito.Desktop.Shell.ViewModels;

public sealed partial class NavigationItemViewModel(
    string titulo, ICommand command, Type tipoDestino, string icono) : ViewModelBase
{
    public string Titulo { get; } = titulo;
    public ICommand Command { get; } = command;
    public Type TipoDestino { get; } = tipoDestino;
    public string Icono { get; } = icono;

    [ObservableProperty]
    private bool _estaSeleccionado;
}
