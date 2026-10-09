using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Domain.Caja.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations
{
    public class SesionCajaConfiguration : IEntityTypeConfiguration<SesionCaja>
    {
        public void Configure(EntityTypeBuilder<SesionCaja> builder)
        {
            builder.ToTable("Sesionescaja");

            builder.HasKey(s => s.Id);

            builder.Property(s => s.FondoInicial).HasColumnType("decimal(18,2)");
            builder.Property(s => s.EfectivoEsperado).HasColumnType("decimal(18,2)");
            builder.Property(s => s.EfectivoReal).HasColumnType("decimal(18,2)");
            builder.Property(s => s.DiferenciaEfectivo).HasColumnType("decimal(18,2)");
            builder.Property(s => s.YapeEsperado).HasColumnType("decimal(18,2)");
            builder.Property(s => s.YapeReal).HasColumnType("decimal(18,2)");
            builder.Property(s => s.DiferenciaYape).HasColumnType("decimal(18,2)");
            builder.Property(s => s.PlinEsperado).HasColumnType("decimal(18,2)");
            builder.Property(s => s.PlinReal).HasColumnType("decimal(18,2)");
            builder.Property(s => s.DiferenciaPlin).HasColumnType("decimal(18,2)");

            builder.Property(s => s.DescuadreResuelto)
                .HasDefaultValue(false);

            builder.Property(s => s.ObservacionResolucionDescuadre)
                .HasMaxLength(300);

            builder.HasMany(s => s.Movimientos)
                .WithOne(m => m.SesionCaja)
                .HasForeignKey(m => m.SesionCajaId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
