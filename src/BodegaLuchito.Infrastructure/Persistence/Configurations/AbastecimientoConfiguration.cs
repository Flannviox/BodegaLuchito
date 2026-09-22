using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using EntidadAbastecimiento = BodegaLuchito.Domain.Abastecimiento.Entities.Abastecimiento;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations;

public sealed class AbastecimientoConfiguration : IEntityTypeConfiguration<EntidadAbastecimiento>
{
    public void Configure(EntityTypeBuilder<EntidadAbastecimiento> builder)
    {
        builder.ToTable("Abastecimiento");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Total).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.MetodoPago).HasConversion<int>().IsRequired();
        builder.Property(x => x.FechaHora).IsRequired();

        // Relación: Un abastecimiento pertenece a un proveedor
        builder.HasOne(x => x.Proveedor)
               .WithMany()
               .HasForeignKey(x => x.ProveedorId)
               .OnDelete(DeleteBehavior.Restrict);

        // Relación: Un abastecimiento tiene muchos detalles (Si se borra el abastecimiento, se borran sus detalles)
        builder.HasMany(x => x.Detalles)
               .WithOne(x => x.Abastecimiento)
               .HasForeignKey(x => x.AbastecimientoId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
