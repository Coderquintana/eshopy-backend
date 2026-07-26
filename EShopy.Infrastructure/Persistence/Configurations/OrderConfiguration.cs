using EShopy.Domain.Orders;
using EShopy.Domain.Tenants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
  public void Configure(EntityTypeBuilder<Order> builder)
  {
    builder.ToTable("Orders", table =>
    {
      table.HasComment("Pedido generado desde checkout.");
      table.HasCheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0");
    });

    builder.HasKey(o => o.Id).HasName("PK_Orders");

    builder.Property(o => o.Id)
      .HasComment("Identificador unico del pedido.");

    builder.Property(o => o.TenantId)
      .HasComment("Identificador del tenant propietario.");

    builder.Property(o => o.StoreId)
      .HasComment("Store al que pertenece el pedido.");

    builder.Property(o => o.OrderNumber)
      .HasComment("Secuencial por tenant. Asignado atomicamente por ICheckoutWriter, no en la creacion.");

    builder.Property(o => o.Status)
      .HasConversion<byte>()
      .HasColumnType("tinyint")
      .HasComment("Estado del pedido (0=PendingPayment,1=Paid,2=Cancelled,3=Refunded).");

    builder.Property(o => o.BuyerEmail)
      .HasMaxLength(200)
      .HasComment("Email del comprador al momento del checkout.");

    builder.Property(o => o.BuyerName)
      .HasMaxLength(200)
      .HasComment("Nombre del comprador al momento del checkout.");

    builder.Property(o => o.ShippingAddress)
      .HasMaxLength(1000)
      .HasComment("Direccion de entrega.");

    builder.Property(o => o.CartToken)
      .HasMaxLength(100)
      .HasComment("CartToken del carrito origen.");

    builder.Property(o => o.CurrencyCode)
      .HasColumnType("char(3)")
      .HasComment("Heredado del Store al momento del checkout.");

    builder.Property(o => o.TotalAmount)
      .HasColumnType("decimal(18,2)")
      .HasComment("Suma de OrderItems, calculado al crear.");

    // Sin FK real: Payment.OrderId ya tiene la FK en la direccion Payment->Order (ver
    // PaymentConfiguration). Un FK real en las dos direcciones seria circular — EF no puede
    // decidir el orden de insercion dentro del mismo SaveChangesAsync. PaymentId aca es una
    // referencia de conveniencia, no la relacion enforced a nivel DB.
    builder.Property(o => o.PaymentId)
      .HasComment("Referencia al Payment activo. Sin FK enforced (ver Payment.OrderId).");

    builder.Property(o => o.CreatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de creacion en UTC.");

    builder.Property(o => o.CreatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que creo el registro.");

    builder.Property(o => o.UpdatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de ultima actualizacion en UTC.");

    builder.Property(o => o.UpdatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que actualizo el registro.");

    builder.Property(o => o.RowVersion)
      .IsRowVersion()
      .HasComment("Token de concurrencia optimista.");

    builder.Property(o => o.Data)
      .HasColumnType("nvarchar(max)")
      .HasComment("JSON para extensiones no criticas.");

    // Coleccion encapsulada, mismo patron que Cart.Items.
    builder.HasMany(o => o.Items)
      .WithOne()
      .HasForeignKey(i => i.OrderId)
      .OnDelete(DeleteBehavior.Cascade);

    builder.Navigation(o => o.Items)
      .HasField("_items")
      .UsePropertyAccessMode(PropertyAccessMode.Field);

    builder.HasIndex(o => new { o.TenantId, o.OrderNumber })
      .IsUnique()
      .HasDatabaseName("UQ_Orders_TenantId_OrderNumber");

    builder.HasIndex(o => new { o.TenantId, o.Status })
      .HasDatabaseName("IX_Orders_TenantId_Status");

    builder.HasIndex(o => new { o.TenantId, o.BuyerEmail })
      .HasDatabaseName("IX_Orders_TenantId_BuyerEmail");

    builder.HasOne<Store>()
      .WithMany()
      .HasForeignKey(o => o.StoreId)
      .HasConstraintName("FK_Orders_Stores_StoreId")
      .OnDelete(DeleteBehavior.Restrict);
  }
}
