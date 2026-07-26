using EShopy.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
  public void Configure(EntityTypeBuilder<Subscription> builder)
  {
    builder.ToTable("Subscriptions", table =>
    {
      table.HasComment("Suscripcion mensual del tenant a un plan.");
      table.HasCheckConstraint("CK_Subscriptions_PriceAmount_NonNegative", "[PriceAmount] >= 0");
    });

    builder.HasKey(s => s.Id).HasName("PK_Subscriptions");

    builder.Property(s => s.Id)
      .HasComment("Identificador unico de la suscripcion.");

    builder.Property(s => s.TenantId)
      .HasComment("Identificador del tenant propietario.");

    builder.Property(s => s.Plan)
      .HasConversion<byte>()
      .HasColumnType("tinyint")
      .HasComment("Plan contratado (0=Basic,1=Gold,2=Diamond).");

    builder.Property(s => s.Status)
      .HasConversion<byte>()
      .HasColumnType("tinyint")
      .HasComment("Estado (0=PendingActivation,1=Active,2=PastDue,3=Suspended,4=Cancelled).");

    builder.Property(s => s.BillingCycleStart)
      .HasColumnType("datetime2")
      .HasComment("Inicio del ciclo de facturacion actual.");

    builder.Property(s => s.BillingCycleEnd)
      .HasColumnType("datetime2")
      .HasComment("Fin del ciclo de facturacion actual.");

    builder.Property(s => s.PriceAmount)
      .HasColumnType("decimal(18,2)")
      .HasComment("Precio del plan al momento de la suscripcion.");

    builder.Property(s => s.CurrencyCode)
      .HasColumnType("char(3)")
      .HasComment("Codigo ISO 4217 de moneda del cobro.");

    builder.Property(s => s.ExternalSubscriptionId)
      .HasMaxLength(100)
      .HasComment("Id en la plataforma de billing externa (Fase 8).");

    builder.Property(s => s.CancelledAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de cancelacion, si aplica.");

    builder.Property(s => s.CreatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de creacion en UTC.");

    builder.Property(s => s.CreatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que creo el registro.");

    builder.Property(s => s.UpdatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de ultima actualizacion en UTC.");

    builder.Property(s => s.UpdatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que actualizo el registro.");

    builder.Property(s => s.RowVersion)
      .IsRowVersion()
      .HasComment("Token de concurrencia optimista.");

    builder.Property(s => s.Data)
      .HasColumnType("nvarchar(max)")
      .HasComment("JSON para extensiones no criticas.");

    // Regla de dominio: no puede haber mas de una suscripcion no cancelada por tenant.
    builder.HasIndex(s => s.TenantId)
      .IsUnique()
      .HasFilter("[Status] <> 4")
      .HasDatabaseName("UQ_Subscriptions_TenantId_NonCancelled");
  }
}
