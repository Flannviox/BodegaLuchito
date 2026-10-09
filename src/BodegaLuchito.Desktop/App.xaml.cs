using System.Windows;
using System.Windows.Threading;
using BodegaLuchito.Application.Autenticacion.UseCases;
using BodegaLuchito.Application.Caja.UseCases;
using BodegaLuchito.Application.Common.Session;
using BodegaLuchito.Application.Productos.UseCases;
using BodegaLuchito.Application.Proveedores.UseCases;
using BodegaLuchito.Application.Ventas.UseCases;
using BodegaLuchito.Desktop.Modules.Abastecimiento.ViewModels; // Agregado
using BodegaLuchito.Desktop.Modules.Abastecimiento.Views; // Agregado
using BodegaLuchito.Desktop.Modules.Autenticacion.ViewModels;
using BodegaLuchito.Desktop.Modules.Autenticacion.Views;
using BodegaLuchito.Desktop.Modules.Caja.ViewModels;
using BodegaLuchito.Desktop.Modules.Inicio.ViewModels;
using BodegaLuchito.Desktop.Modules.Productos.ViewModels;
using BodegaLuchito.Desktop.Modules.Proveedores.ViewModels;
using BodegaLuchito.Desktop.Modules.Ventas.ViewModels;
using BodegaLuchito.Desktop.Navigation;
using BodegaLuchito.Desktop.Shell.ViewModels;
using BodegaLuchito.Infrastructure;
using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BodegaLuchito.Desktop;

public partial class App : System.Windows.Application
{
    private readonly ServiceProvider _serviceProvider;
    private AutenticacionWindow? _autenticacionWindow;

    public App()
    {
        var services = new ServiceCollection();

        ConfigureServices(services);

        _serviceProvider =
            services.BuildServiceProvider();

        var sesionUsuario =
            _serviceProvider.GetRequiredService<ISesionUsuario>();

        sesionUsuario.SesionCambiada +=
            OnSesionCambiada;

        DispatcherUnhandledException +=
            OnDispatcherUnhandledException;
    }

    private static void ConfigureServices(
    IServiceCollection services)
    {
        // Infrastructure
        services.AddInfrastructure();

        // Sesion
        services.AddSingleton<ISesionUsuario, SesionUsuario>();

        // Navegacion
        services.AddSingleton<INavigationService, NavigationService>();

        // Shell
        services.AddSingleton<MainWindowViewModel>();
        services.AddTransient<MainWindow>();

        // Autenticacion
        services.AddTransient<RequiereConfiguracionInicialUseCase>();
        services.AddTransient<IniciarSesionUseCase>();
        services.AddTransient<CrearAdministradorInicialUseCase>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<ConfiguracionInicialViewModel>();
        services.AddTransient<AutenticacionWindowViewModel>();

        services.AddTransient<RegistrarUsuarioUseCase>();
        services.AddTransient<ListarUsuariosUseCase>();
        services.AddTransient<ModificarUsuarioUseCase>();
        services.AddTransient<CambiarEstadoUsuarioUseCase>();
        services.AddTransient<RestablecerPasswordUsuarioUseCase>();
        services.AddTransient<UsuariosViewModel>();

        services.AddTransient<AutenticacionWindow>();

        // Modulos
        services.AddTransient<InicioViewModel>();
        services.AddTransient<BodegaLuchito.Desktop.Modules.Inventario.ViewModels.InventarioViewModel>();
        services.AddTransient<ProductosViewModel>();
        services.AddTransient<VentasViewModel>();
        services.AddTransient<ProveedoresViewModel>();
        services.AddTransient<CajaViewModel>();
        services.AddTransient<RegistrarProveedorUseCase>();
        services.AddTransient<AbrirCajaUseCase>();
        services.AddTransient<CerrarCajaUseCase>();
        services.AddTransient<ConsultarHistorialCierresUseCase>();
        services.AddTransient<RegistrarVentaUseCase>();

        // Abastecimiento
        services.AddTransient<AbastecimientoView>();
        services.AddTransient<AbastecimientoViewModel>();

        //Productos - Caso de uso y ViewModel

        services.AddTransient<RegistrarProductoUseCase>();
        services.AddTransient<ModificarProductoUseCase>();
        services.AddTransient<EliminarProductoUseCase>();
        services.AddTransient<ReactivarProductoUseCase>();
        services.AddTransient<ConsultarHistorialPreciosProductoUseCase>();
        services.AddTransient<ConsultarHistorialProductoUseCase>();
        services.AddTransient<ConsultarHistorialGeneralProductosUseCase>();
        services.AddTransient<RegistrarProductoViewModel>();

        // Categorías - Casos de uso
        services.AddTransient<ObtenerCategoriasActivasUseCase>(); 
        services.AddTransient<CrearCategoriaUseCase>();
        //Caja - Casos de uso
        services.AddTransient<AbrirCajaUseCase>();
        services.AddTransient<CerrarCajaUseCase>();
        services.AddTransient<ConsultarHistorialCierresUseCase>();
        services.AddTransient<ConsultarDetalleMovimientosCajaUseCase>();
        services.AddTransient<ResolverDescuadreCajaUseCase>();
    }

    protected override async void OnStartup(
     StartupEventArgs e)
    {
        base.OnStartup(e);

        try
        {
            var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<BodegaLuchitoDbContext>>();
            await using var context = await contextFactory.CreateDbContextAsync();
            await context.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"No se pudo preparar la base de datos: {ex.Message}",
                "Bodega Luchito",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
            return;
        }

        await MostrarAutenticacionAsync();
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
    private async Task MostrarAutenticacionAsync()
    {
        if (_autenticacionWindow is not null)
        {
            _autenticacionWindow.Activate();
            return;
        }

        var autenticacionWindow =
            _serviceProvider
                .GetRequiredService<AutenticacionWindow>();

        _autenticacionWindow =
            autenticacionWindow;

        var viewModel =
            (AutenticacionWindowViewModel)
                autenticacionWindow.DataContext;

        viewModel.AutenticacionCompletada += () =>
        {
            var mainWindowViewModel = _serviceProvider.GetRequiredService<MainWindowViewModel>();

            mainWindowViewModel.InicializarParaSesionActual();
            var mainWindow =
                _serviceProvider.GetRequiredService<MainWindow>();

            MainWindow = mainWindow;

            mainWindow.Show();

            autenticacionWindow.Close();

            _autenticacionWindow = null;
        };

        autenticacionWindow.Closed += (_, _) =>
        {
            _autenticacionWindow = null;
        };

        await autenticacionWindow.InicializarAsync();

        autenticacionWindow.Show();
    }
    private async void OnSesionCambiada()
    {
        var sesionUsuario =
            _serviceProvider.GetRequiredService<ISesionUsuario>();

        if (sesionUsuario.EstaAutenticado)
        {
            return;
        }

        if (MainWindow is not null)
        {
            MainWindow.Close();
            MainWindow = null;
        }

        await MostrarAutenticacionAsync();
    }


}
