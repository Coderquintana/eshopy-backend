using EShopy.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
  public void Configure(EntityTypeBuilder<Cart> builder)
  {
    builder.ToTable("Carts", table =>
    {
      table.HasComment("Carrito server-side, previo al checkout.");
    });

    builder.HasKey(c => c.Id).HasName("PK_Carts");

    builder.Property(c => c.Id)
      .HasComment("Identificador unico del carrito.");

    builder.Property(c => c.TenantId)
      .HasComment("Identificador del tenant propietario.");

    builder.Property(c => c.CartToken)
      .HasMaxLength(100)
      .HasComment("UUID generado en el frontend.");

    builder.Property(c => c.ExpiresAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Se extiende con cada actividad. Para limpieza de carritos abandonados (F6-04).");

    builder.Property(c => c.CreatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de creacion en UTC.");

    builder.Property(c => c.CreatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que creo el registro.");

    builder.Property(c => c.UpdatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de ultima actualizacion en UTC.");

    builder.Property(c => c.UpdatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que actualizo el registro.");

    builder.Property(c => c.RowVersion)
      .IsRowVersion()
      .HasComment("Token de concurrencia optimista.");

    builder.Property(c => c.Data)
      .HasColumnType("nvarchar(max)")
      .HasComment("JSON para extensiones no criticas.");

    // Coleccion encapsulada: Items expone IReadOnlyList (sin setter) respaldado por el campo
    // privado _items — EF necesita PropertyAccessMode.Field para materializarla/trackearla.
    builder.HasMany(c => c.Items)
      .WithOne()
      .HasForeignKey(i => i.CartId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(c => c.Items)
      .HasField("_items")
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(c => new { c.TenantId, c.CartToken })
      .IsUnique()
      .HasDatabaseName("UQ_Carts_TenantId_CartToken");
  }
}
