using EShopy.Domain.Common.Counters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class TenantCounterConfiguration : IEntityTypeConfiguration<TenantCounter>
{
  public void Configure(EntityTypeBuilder<TenantCounter> builder)
  {
    builder.ToTable("TenantCounters", table =>
    {
      table.HasComment("Contadores atomicos por tenant (ej. secuencia de OrderNumber).");
    });

    builder.HasKey(c => new { c.TenantId, c.CounterType }).HasName("PK_TenantCounters");

    builder.Property(c => c.TenantId)
      .HasComment("Identificador del tenant propietario.");

    builder.Property(c => c.CounterType)
      .HasMaxLength(50)
      .HasComment("Tipo de contador (ej. 'OrderNumber').");

    // Concurrency token EF: el UPDATE generado incluye "WHERE CurrentValue = @original". Dos
    // checkouts que compiten por el mismo contador — el perdedor recibe DbUpdateConcurrencyException
    // y reintenta (ver EfCheckoutWriter). Reemplaza el UPDLOCK/ROWLOCK descartado en GOVERNANCE.md.
    builder.Property(c => c.CurrentValue)
      .IsConcurrencyToken()
      .HasComment("Valor actual del contador. Concurrency token EF — sin SQL crudo.");
  }
}
