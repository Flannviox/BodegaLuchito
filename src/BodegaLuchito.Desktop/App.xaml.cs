using System.Windows;
using BodegaLuchito.Desktop.Modules.Inicio.ViewModels;
using BodegaLuchito.Desktop.Modules.Productos.ViewModels;
using BodegaLuchito.Desktop.Modules.Ventas.ViewModels;
using BodegaLuchito.Desktop.Navigation;
using BodegaLuchito.Desktop.Shell.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace BodegaLuchito.Desktop;

public partial class App : System.Windows.Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();
    }

    private static void ConfigureServices(
    IServiceCollection services)
    {
        // Navegación
        services.AddSingleton<INavigationService, NavigationService>();

        // Shell
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        // Módulos
        services.AddTransient<InicioViewModel>();
        services.AddTransient<ProductosViewModel>();
        services.AddTransient<VentasViewModel>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();

        base.OnExit(e);
    }
}
