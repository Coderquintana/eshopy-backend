using EShopy.Domain.Common.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
  public void Configure(EntityTypeBuilder<AuditLog> builder)
  {
    builder.ToTable("AuditLogs", table =>
    {
      table.HasComment("Registro append-only de operaciones sensibles (F9-03). Nunca se actualiza ni se borra.");
    });

    builder.HasKey(a => a.Id).HasName("PK_AuditLogs");

    builder.Property(a => a.Id)
      .HasComment("Identificador unico del registro.");

    builder.Property(a => a.TenantId)
      .HasComment("Tenant afectado por la accion. Nullable: hay acciones a nivel plataforma (ej. activacion de tenant via SUPERADMIN).");

    builder.Property(a => a.UserId)
      .HasMaxLength(100)
      .HasComment("Id del usuario autenticado que ejecuto la accion. Null si no hay usuario autenticado (ej. webhook).");

    builder.Property(a => a.UserEmail)
      .HasMaxLength(200)
      .HasComment("Email del usuario autenticado. Null si no hay usuario autenticado.");

    builder.Property(a => a.Action)
      .HasMaxLength(100)
      .HasComment("Modulo.Operacion, ej. 'Order.ChangeStatus'.");

    builder.Property(a => a.EntityType)
      .HasMaxLength(100)
      .HasComment("Tipo de entidad afectada, ej. 'Order'.");

    builder.Property(a => a.EntityId)
      .HasComment("Id de la entidad afectada.");

    builder.Property(a => a.Details)
      .HasMaxLength(1000)
      .HasComment("Texto libre con el detalle de la accion (ej. cambio de estado). No es JSON estructurado a proposito — YAGNI hasta que un caso de uso real lo pida.");

    builder.Property(a => a.CreatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de la accion en UTC.");

    builder.HasIndex(a => new { a.TenantId, a.CreatedAtUtc })
      .HasDatabaseName("IX_AuditLogs_TenantId_CreatedAtUtc");

    builder.HasIndex(a => new { a.EntityType, a.EntityId })
      .HasDatabaseName("IX_AuditLogs_EntityType_EntityId");
  }
}
