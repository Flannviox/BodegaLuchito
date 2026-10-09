using System.Windows;
using System.Windows.Controls;
using BodegaLuchito.Desktop.Modules.BI.ViewModels;

namespace BodegaLuchito.Desktop.Modules.BI.Views;

public partial class PrediccionDemandaView : UserControl
{
    public PrediccionDemandaView() => InitializeComponent();

    private void AlSalir(object sender, RoutedEventArgs e)
    {
        if (DataContext is PrediccionDemandaViewModel vm) vm.CancelarCommand.Execute(null);
    }
}
