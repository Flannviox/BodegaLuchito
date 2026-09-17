using BodegaLuchito.Domain.Proveedores.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations;

public sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("Proveedor");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Nombre).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Ruc).IsRequired().HasMaxLength(11);
        builder.HasIndex(x => x.Ruc).IsUnique();

        builder.Property(x => x.Telefono).HasMaxLength(10);
        builder.Property(x => x.Direccion).HasMaxLength(200);
        builder.Property(x => x.Activo).IsRequired();
        builder.Property(x => x.FechaCreacion).IsRequired();
        builder.Property(x => x.FechaActualizacion);
    }
}
