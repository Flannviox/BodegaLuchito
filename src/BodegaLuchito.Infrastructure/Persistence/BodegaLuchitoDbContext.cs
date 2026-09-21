using BodegaLuchito.Domain.Productos.Entities;
using Microsoft.EntityFrameworkCore;

namespace BodegaLuchito.Infrastructure.Persistence;

public sealed class BodegaLuchitoDbContext
    : DbContext
{
    public BodegaLuchitoDbContext(
        DbContextOptions<BodegaLuchitoDbContext> options)
        : base(options)
    {
    }

    public DbSet<Producto> Productos =>
        Set<Producto>();

    public DbSet<Categoria> Categorias =>
        Set<Categoria>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BodegaLuchitoDbContext).Assembly);
    }
}
