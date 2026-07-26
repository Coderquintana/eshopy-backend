using EShopy.Domain.Orders;
using EShopy.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
  public void Configure(EntityTypeBuilder<OrderItem> builder)
  {
    builder.ToTable("OrderItems", table =>
    {
      table.HasComment("Snapshot inmutable de un producto al momento del checkout.");
      table.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] >= 1");
      table.HasCheckConstraint("CK_OrderItems_UnitPrice_NonNegative", "[UnitPrice] >= 0");
    });

    builder.HasKey(i => i.Id).HasName("PK_OrderItems");

    builder.Property(i => i.Id)
      .HasComment("Identificador unico del item.");

    builder.Property(i => i.OrderId)
      .HasComment("Pedido al que pertenece.");

    builder.Property(i => i.ProductId)
      .HasComment("Producto referenciado (FK a Products). Referencia historica, no se borra si el producto se archiva.");

    builder.Property(i => i.ProductName)
      .HasMaxLength(300)
      .HasComment("Snapshot del nombre al momento del checkout.");

    builder.Property(i => i.ProductSku)
      .HasMaxLength(64)
      .HasComment("Snapshot del SKU al momento del checkout.");

    builder.Property(i => i.UnitPrice)
      .HasColumnType("decimal(18,2)")
      .HasComment("Snapshot del precio unitario al momento del checkout.");

    builder.Property(i => i.Quantity)
      .HasComment("Cantidad del item.");

    // Calculado (UnitPrice * Quantity), no persistido — nunca puede quedar desincronizado.
    builder.Ignore(i => i.Subtotal);

    builder.HasOne<Product>()
      .WithMany()
      .HasForeignKey(i => i.ProductId)
      .HasConstraintName("FK_OrderItems_Products_ProductId")
      .OnDelete(DeleteBehavior.Restrict);
  }
}
