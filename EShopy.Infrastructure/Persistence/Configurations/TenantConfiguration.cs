using EShopy.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
  public void Configure(EntityTypeBuilder<Tenant> builder)
  {
    builder.ToTable("Tenants", table =>
    {
      table.HasComment("Tenants de la plataforma. Entidad global, no multi-tenant.");
    });

    builder.HasKey(t => t.Id).HasName("PK_Tenants");

    builder.Property(t => t.Id)
      .HasComment("Identificador unico del tenant.");

    builder.Property(t => t.Subdomain)
      .HasMaxLength(50)
      .HasComment("Subdominio unico en toda la plataforma.");

    builder.Property(t => t.BusinessName)
      .HasMaxLength(200)
      .HasComment("Nombre legal del negocio.");

    builder.Property(t => t.Status)
      .HasConversion<byte>()
      .HasColumnType("tinyint")
      .HasComment("Estado del tenant (0=PendingPayment,1=Active,2=Suspended,3=Cancelled).");

    builder.Property(t => t.Plan)
      .HasConversion<byte>()
      .HasColumnType("tinyint")
      .HasComment("Plan contratado (0=Basic,1=Gold,2=Diamond).");

    builder.Property(t => t.CreatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de alta en UTC.");

    builder.Property(t => t.UpdatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de ultimo cambio de estado en UTC.");

    builder.Property(t => t.ActivatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha en que paso a Active por primera vez.");

    builder.HasIndex(t => t.Subdomain)
      .IsUnique()
      .HasDatabaseName("UQ_Tenants_Subdomain");
  }
}
