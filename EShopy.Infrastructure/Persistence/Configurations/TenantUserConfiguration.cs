using EShopy.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class TenantUserConfiguration : IEntityTypeConfiguration<TenantUser>
{
  public void Configure(EntityTypeBuilder<TenantUser> builder)
  {
    builder.ToTable("TenantUsers", table =>
    {
      table.HasComment("Usuarios con acceso al panel de administracion de un tenant.");
    });

    builder.HasKey(u => u.Id).HasName("PK_TenantUsers");

    builder.Property(u => u.Id)
      .HasComment("Identificador unico del usuario.");

    builder.Property(u => u.TenantId)
      .HasComment("Identificador del tenant propietario.");

    builder.Property(u => u.KeycloakUserId)
      .HasMaxLength(100)
      .HasComment("Id del usuario en Keycloak.");

    builder.Property(u => u.Email)
      .HasMaxLength(200)
      .HasComment("Email del usuario. Unico por tenant.");

    builder.Property(u => u.Name)
      .HasMaxLength(200)
      .HasComment("Nombre visible del usuario.");

    builder.Property(u => u.Role)
      .HasConversion<byte>()
      .HasColumnType("tinyint")
      .HasComment("Rol del usuario (0=Owner,1=Admin,2=Staff).");

    builder.Property(u => u.IsActive)
      .HasComment("Permite deshabilitar el acceso sin eliminar el registro.");

    builder.Property(u => u.CreatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de creacion en UTC.");

    builder.Property(u => u.CreatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que creo el registro.");

    builder.Property(u => u.UpdatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de ultima actualizacion en UTC.");

    builder.Property(u => u.UpdatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que actualizo el registro.");

    builder.Property(u => u.RowVersion)
      .IsRowVersion()
      .HasComment("Token de concurrencia optimista.");

    builder.Property(u => u.Data)
      .HasColumnType("nvarchar(max)")
      .HasComment("JSON para extensiones no criticas.");

    builder.HasIndex(u => new { u.TenantId, u.Email })
      .IsUnique()
      .HasDatabaseName("UQ_TenantUsers_TenantId_Email");
  }
}
