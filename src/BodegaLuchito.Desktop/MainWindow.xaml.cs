using System.Windows;
using BodegaLuchito.Application.Caja.UseCases;
using BodegaLuchito.Desktop.Modules.Caja.ViewModels;
using BodegaLuchito.Desktop.Modules.Caja.Views;
using BodegaLuchito.Desktop.Shell.ViewModels;
using BodegaLuchito.Infrastructure.Caja.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Desktop;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

    }
}
