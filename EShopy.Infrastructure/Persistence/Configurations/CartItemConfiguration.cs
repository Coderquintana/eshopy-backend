using EShopy.Domain.Carts;
using EShopy.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
  public void Configure(EntityTypeBuilder<CartItem> builder)
  {
    builder.ToTable("CartItems", table =>
    {
      table.HasComment("Items de un carrito. Un producto, una fila — sin snapshot de precio.");
      table.HasCheckConstraint("CK_CartItems_Quantity_Positive", "[Quantity] >= 1");
    });

    builder.HasKey(i => i.Id).HasName("PK_CartItems");

    builder.Property(i => i.Id)
      .HasComment("Identificador unico del item.");

    builder.Property(i => i.CartId)
      .HasComment("Carrito al que pertenece.");

    builder.Property(i => i.ProductId)
      .HasComment("Producto referenciado (FK a Products).");

    builder.Property(i => i.Quantity)
      .HasComment("Cantidad. Acumula si el producto se agrega de nuevo, no duplica fila.");

    builder.Property(i => i.CreatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de creacion en UTC.");

    builder.Property(i => i.UpdatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de ultima actualizacion en UTC.");

    builder.HasIndex(i => new { i.CartId, i.ProductId })
      .IsUnique()
      .HasDatabaseName("UQ_CartItems_CartId_ProductId");

    builder.HasOne<Product>()
      .WithMany()
      .HasForeignKey(i => i.ProductId)
      .HasConstraintName("FK_CartItems_Products_ProductId")
      .OnDelete(DeleteBehavior.Restrict);
  }
}
