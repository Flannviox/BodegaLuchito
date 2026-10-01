using BodegaLuchito.Domain.Inventario.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations;

public sealed class MovimientoInventarioConfiguration : IEntityTypeConfiguration<MovimientoInventario>
{
    public void Configure(EntityTypeBuilder<MovimientoInventario> builder)
    {
        builder.ToTable("MovimientosInventario");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Tipo)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Cantidad).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.StockAnterior).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.StockPosterior).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.FechaHora).IsRequired();
        builder.Property(x => x.Descripcion).HasMaxLength(250);

        builder.HasIndex(x => new { x.ProductoId, x.FechaHora });

        builder.HasOne(x => x.Producto)
            .WithMany()
            .HasForeignKey(x => x.ProductoId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
