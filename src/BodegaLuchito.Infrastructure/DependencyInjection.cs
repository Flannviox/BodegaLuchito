using BodegaLuchito.Application.Autenticacion.Interfaces;
using BodegaLuchito.Infrastructure.Autenticacion.Repositories;
using BodegaLuchito.Infrastructure.Autenticacion.Security;
using BodegaLuchito.Infrastructure.Persistence;
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

        return services;
    }
}
