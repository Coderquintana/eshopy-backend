using EShopy.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
  public void Configure(EntityTypeBuilder<Store> builder)
  {
    builder.ToTable("Stores", table =>
    {
      table.HasComment("Configuracion de tienda por tenant. 1:1 con Tenant en MVP.");
    });

    builder.HasKey(s => s.Id).HasName("PK_Stores");

    builder.Property(s => s.Id)
      .HasComment("Identificador unico del store.");

    builder.Property(s => s.TenantId)
      .HasComment("Identificador del tenant propietario.");

    builder.Property(s => s.Name)
      .HasMaxLength(200)
      .HasComment("Nombre publico de la tienda.");

    builder.Property(s => s.CurrencyCode)
      .HasColumnType("char(3)")
      .HasComment("Codigo ISO 4217 de moneda. Heredado por Products y Orders.");

    builder.Property(s => s.Timezone)
      .HasMaxLength(100)
      .HasComment("Timezone IANA de la tienda, ej. 'America/Asuncion'.");

    builder.Property(s => s.PrimaryColor)
      .HasMaxLength(7)
      .HasComment("Color primario de marca en hex, ej. '#FF5733'.");

    builder.Property(s => s.LogoUrl)
      .HasMaxLength(500)
      .HasComment("URL del logo de la tienda.");

    builder.Property(s => s.BackgroundColor)
      .HasMaxLength(7)
      .HasComment("Color de fondo en hex.");

    builder.Property(s => s.Description)
      .HasMaxLength(1000)
      .HasComment("Descripcion publica de la tienda.");

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

    builder.HasIndex(s => s.TenantId)
      .IsUnique()
      .HasDatabaseName("UQ_Stores_TenantId");
  }
}
