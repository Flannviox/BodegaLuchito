using BodegaLuchito.Application.Abastecimiento.Interfaces;
using BodegaLuchito.Application.Abastecimiento.UseCases;
using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Application.Caja.Interfaces;
using BodegaLuchito.Application.Inventario.Interfaces;
using BodegaLuchito.Application.Inventario.UseCases;
using BodegaLuchito.Application.Productos.Interfaces;
using BodegaLuchito.Application.Proveedores.Interfaces;
using BodegaLuchito.Application.Proveedores.UseCases;
using BodegaLuchito.Infrastructure.Abastecimiento.Repositories;
using BodegaLuchito.Infrastructure.Autenticacion.Repositories;
using BodegaLuchito.Infrastructure.Autenticacion.Security;
using BodegaLuchito.Infrastructure.Caja.Repositories;
using BodegaLuchito.Infrastructure.Inventario.Repositories;
using BodegaLuchito.Infrastructure.Persistence;
using BodegaLuchito.Infrastructure.Productos.Repositories;
using BodegaLuchito.Infrastructure.Proveedores.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BodegaLuchito.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services)
    {
        var connectionString =
            DatabasePathProvider.GetConnectionString();

        services.AddDbContextFactory<BodegaLuchitoDbContext>(options => options.UseSqlite(connectionString));

        services.AddTransient<IUsuarioRepository, UsuarioRepository>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        services.AddTransient<IProductoRepository, ProductoRepository>();
        services.AddTransient<IProveedorRepository, ProveedorRepository>();
        services.AddTransient<EditarProveedorUseCase>();
        services.AddTransient<CambiarEstadoProveedorUseCase>();
        services.AddTransient<EliminarProveedorUseCase>();
        services.AddTransient<ICajaRepository, CajaRepository>();
        services.AddTransient<ICategoriaRepository, CategoriaRepository>();
        services.AddTransient<IAbastecimientoRepository, AbastecimientoRepository>();
        services.AddTransient<RegistrarAbastecimientoUseCase>();
        services.AddTransient<ConsultarHistorialAbastecimientosUseCase>();
        services.AddTransient<IInventarioRepository, InventarioRepository>();
        services.AddTransient<ConsultarMovimientosInventarioUseCase>();
        services.AddTransient<GestionarInventarioUseCase>();


        return services;
    }
}
