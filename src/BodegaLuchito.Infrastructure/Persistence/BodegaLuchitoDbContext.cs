using BodegaLuchito.Domain.Abastecimiento.Entities;
using BodegaLuchito.Domain.Inventario.Entities;
using BodegaLuchito.Domain.Productos.Entities;
using Microsoft.EntityFrameworkCore;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;
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

    public DbSet<HistorialPrecioProducto> HistorialPreciosProducto =>
        Set<HistorialPrecioProducto>();

    public DbSet<HistorialActividadProducto> HistorialActividadProducto =>
        Set<HistorialActividadProducto>();

    public DbSet<Categoria> Categorias =>
        Set<Categoria>();

    public DbSet<EntidadAbastecimiento> Abastecimientos =>
        Set<EntidadAbastecimiento>();
    public DbSet<DetalleAbastecimiento> DetallesAbastecimiento =>
        Set<DetalleAbastecimiento>();

    public DbSet<MovimientoInventario> MovimientosInventario =>
        Set<MovimientoInventario>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(BodegaLuchitoDbContext).Assembly);
    }
}
