using BodegaLuchito.Domain.Abastecimiento.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations;

public sealed class DetalleAbastecimientoConfiguration : IEntityTypeConfiguration<DetalleAbastecimiento>
{
    public void Configure(EntityTypeBuilder<DetalleAbastecimiento> builder)
    {
        builder.ToTable("DetalleAbastecimiento");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Cantidad).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CostoUnitario)
            .HasColumnName("PrecioUnitario")
            .HasColumnType("decimal(18,4)")
            .IsRequired();

        builder.Property(x => x.TotalLinea)
            .HasColumnName("Subtotal")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // Relación: Un detalle pertenece a un producto (No se puede borrar un producto si tiene historial de abastecimiento)
        builder.HasOne(x => x.Producto)
               .WithMany()
               .HasForeignKey(x => x.ProductoId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
