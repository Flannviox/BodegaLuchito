using BodegaLuchito.Domain.Productos.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations;

public sealed class ProductoConfiguration
    : IEntityTypeConfiguration<Producto>
{
    public void Configure(
        EntityTypeBuilder<Producto> builder)
    {
        builder.ToTable("Producto");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Categoria)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.CodigoBarras)
            .HasMaxLength(50);

        builder.HasIndex(x => x.CodigoBarras)
            .IsUnique();

        builder.Property(x => x.PrecioVenta)
            .IsRequired();

        builder.Property(x => x.UnidadVenta)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ControlaInventario)
            .IsRequired();

        builder.Property(x => x.StockActual)
            .IsRequired();

        builder.Property(x => x.StockMinimo)
            .IsRequired();

        builder.Property(x => x.Activo)
            .IsRequired();

        builder.Property(x => x.FechaCreacion)
            .IsRequired();
    }
}
