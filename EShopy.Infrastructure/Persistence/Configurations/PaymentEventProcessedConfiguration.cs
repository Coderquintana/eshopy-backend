using EShopy.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class PaymentEventProcessedConfiguration : IEntityTypeConfiguration<PaymentEventProcessed>
{
  public void Configure(EntityTypeBuilder<PaymentEventProcessed> builder)
  {
    builder.ToTable("PaymentEventsProcessed", table =>
    {
      table.HasComment("Ledger de idempotencia de webhooks de pago. Global, no multi-tenant: (Provider, EventId) ya es unico a nivel provider.");
    });

    builder.HasKey(e => e.Id).HasName("PK_PaymentEventsProcessed");

    builder.Property(e => e.Id)
      .HasComment("Identificador unico del registro.");

    builder.Property(e => e.Provider)
      .HasMaxLength(50)
      .HasComment("Provider de pago que emitio el evento.");

    builder.Property(e => e.EventId)
      .HasMaxLength(200)
      .HasComment("Id del evento segun el provider.");

    builder.Property(e => e.ProcessedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de procesamiento en UTC.");

    builder.HasIndex(e => new { e.Provider, e.EventId })
      .IsUnique()
      .HasDatabaseName("UQ_PaymentEventsProcessed_Provider_EventId");
  }
}
