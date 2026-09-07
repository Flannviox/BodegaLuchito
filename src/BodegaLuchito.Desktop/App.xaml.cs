using System.Windows;
using BodegaLuchito.Desktop.Modules.Inicio.ViewModels;
using BodegaLuchito.Desktop.Modules.Productos.ViewModels;
using BodegaLuchito.Desktop.Modules.Ventas.ViewModels;
using BodegaLuchito.Desktop.Navigation;
using BodegaLuchito.Desktop.Shell.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using BodegaLuchito.Application.Common.Session;
using System.Windows.Threading;
using BodegaLuchito.Infrastructure;

namespace BodegaLuchito.Desktop;

public partial class App : System.Windows.Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        _serviceProvider =
            services.BuildServiceProvider();

        DispatcherUnhandledException +=
            OnDispatcherUnhandledException;
    }

    private static void ConfigureServices(
    IServiceCollection services)
    {
        // Infrastructure
        services.AddInfrastructure();

        //Sesion
        services.AddSingleton<ISesionUsuario, SesionUsuario>();

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

    protected override void OnStartup(
    StartupEventArgs e)
    {
        base.OnStartup(e);

#if DEBUG

        var sesion =
            _serviceProvider.GetRequiredService<ISesionUsuario>();

        sesion.IniciarSesion(
            new UsuarioSesion
            {
                IdUsuario = 1,
                NombreUsuario = "Usuario Desarrollo",
                NombreRol = "Administrador"
            });

#endif

        var mainWindow =
            _serviceProvider.GetRequiredService<MainWindow>();

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider.Dispose();

        base.OnExit(e);
    }
    private static void OnDispatcherUnhandledException(
    object sender,
    DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show(
            "Ocurrio un error inesperado en la aplicacion.",
            "Bodega Luchito",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

#if DEBUG

        e.Handled = false;

#else

    e.Handled = true;

#endif
    }
}
