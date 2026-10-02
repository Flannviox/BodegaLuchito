using BodegaLuchito.Domain.Ventas.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations;

public sealed class DetalleVentaConfiguration : IEntityTypeConfiguration<DetalleVenta>
{
    public void Configure(EntityTypeBuilder<DetalleVenta> builder)
    {
        builder.ToTable("DetallesVenta");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Cantidad).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.PrecioUnitario).HasColumnType("decimal(18,4)").IsRequired();
        builder.Property(x => x.Subtotal).HasColumnType("decimal(18,2)").IsRequired();

        builder.HasOne(x => x.Producto)
               .WithMany()
               .HasForeignKey(x => x.ProductoId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
