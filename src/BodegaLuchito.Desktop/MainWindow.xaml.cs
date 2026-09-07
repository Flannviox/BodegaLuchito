using System.Windows;
using BodegaLuchito.Desktop.Shell.ViewModels;

namespace BodegaLuchito.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;
    }
}
