using BodegaLuchito.Domain.Ventas.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations;

public sealed class VentaConfiguration : IEntityTypeConfiguration<Venta>
{
    public void Configure(EntityTypeBuilder<Venta> builder)
    {
        builder.ToTable("Ventas");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Total).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Subtotal).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.IGV).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.MetodoPago).HasConversion<int>().IsRequired();
        builder.Property(x => x.FechaHora).IsRequired();

        // Relación con el usuario que hace la venta
        builder.HasOne(x => x.Usuario)
               .WithMany()
               .HasForeignKey(x => x.UsuarioId)
               .OnDelete(DeleteBehavior.Restrict);

        // Si se borra la venta (aunque por regla no se debe), se borran los detalles
        builder.HasMany(x => x.Detalles)
               .WithOne(x => x.Venta)
               .HasForeignKey(x => x.VentaId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
