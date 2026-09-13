using System;
using System.Collections.Generic;
using System.Text;
using BodegaLuchito.Domain.Caja.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BodegaLuchito.Infrastructure.Persistence.Configurations
{
    public class MovimientoCajaConfiguration : IEntityTypeConfiguration<MovimientoCaja>
    {
        public void Configure(EntityTypeBuilder<MovimientoCaja> builder)
        {
            builder.ToTable("MovimientosCaja");

            builder.HasKey(m => m.Id);

            builder.Property(m => m.Monto).HasColumnType("decimal(18,2)");
        }


    }
}
