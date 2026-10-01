using BodegaLuchito.Domain.Productos.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations;

public sealed class HistorialActividadProductoConfiguration
    : IEntityTypeConfiguration<HistorialActividadProducto>
{
    public void Configure(EntityTypeBuilder<HistorialActividadProducto> builder)
    {
        builder.ToTable("HistorialActividadProducto");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tipo)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.Descripcion)
            .HasMaxLength(250)
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
