using BodegaLuchito.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Tests.Helpers;

public sealed class TestDbContextFactory
    : IDbContextFactory<BodegaLuchitoDbContext>
{
    private readonly DbContextOptions<BodegaLuchitoDbContext> _options;

    public TestDbContextFactory(
        DbContextOptions<BodegaLuchitoDbContext> options)
    {
        _options = options;
    }

    public BodegaLuchitoDbContext CreateDbContext()
    {
        return new BodegaLuchitoDbContext(_options);
    }

    public Task<BodegaLuchitoDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new BodegaLuchitoDbContext(_options));
    }
}
