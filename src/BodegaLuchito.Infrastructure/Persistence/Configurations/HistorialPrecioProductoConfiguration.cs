using BodegaLuchito.Domain.Productos.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations;

public sealed class HistorialPrecioProductoConfiguration
    : IEntityTypeConfiguration<HistorialPrecioProducto>
{
    public void Configure(EntityTypeBuilder<HistorialPrecioProducto> builder)
    {
        builder.ToTable("HistorialPreciosProducto");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PrecioAnterior)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.PrecioNuevo)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(x => x.FechaHora)
            .IsRequired();

        builder.Property(x => x.UsuarioNombre)
            .HasMaxLength(120)
            .IsRequired();

        builder.HasIndex(x => new { x.ProductoId, x.FechaHora });

        builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
