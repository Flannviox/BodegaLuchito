using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BodegaLuchito.Infrastructure.Persistence;

public sealed class BodegaLuchitoDbContextFactory
    : IDesignTimeDbContextFactory<BodegaLuchitoDbContext>
{
    public BodegaLuchitoDbContext CreateDbContext(
        string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<BodegaLuchitoDbContext>();

        optionsBuilder.UseSqlite(
            DatabasePathProvider.GetConnectionString());

        return new BodegaLuchitoDbContext(
            optionsBuilder.Options);
    }
}
