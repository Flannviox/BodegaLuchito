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

        services.AddDbContextFactory<BodegaLuchitoDbContext>(
            options =>
                options.UseSqlite(connectionString));

        return services;
    }
}
