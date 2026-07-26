using EShopy.Domain.Orders;
using EShopy.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
  public void Configure(EntityTypeBuilder<Payment> builder)
  {
    builder.ToTable("Payments", table =>
    {
      table.HasComment("Intento de pago asociado a un Order.");
      table.HasCheckConstraint("CK_Payments_Amount_NonNegative", "[Amount] >= 0");
    });

    builder.HasKey(p => p.Id).HasName("PK_Payments");

    builder.Property(p => p.Id)
      .HasComment("Identificador unico del pago.");

    builder.Property(p => p.TenantId)
      .HasComment("Identificador del tenant propietario.");

    builder.Property(p => p.OrderId)
      .HasComment("Pedido al que pertenece (FK a Orders).");

    builder.Property(p => p.Status)
      .HasConversion<byte>()
      .HasColumnType("tinyint")
      .HasComment("Estado del pago (0=Initiated,1=Authorized,2=Captured,3=Failed,4=Refunded).");

    builder.Property(p => p.Provider)
      .HasMaxLength(50)
      .HasComment("Provider de pago (ej. 'bancard', 'pagopar', 'fake').");

    builder.Property(p => p.ProviderPaymentId)
      .HasMaxLength(200)
      .HasComment("Id de la transaccion en el provider.");

    builder.Property(p => p.ProviderPaymentUrl)
      .HasMaxLength(1000)
      .HasComment("URL de pago devuelta al frontend.");

    builder.Property(p => p.Amount)
      .HasColumnType("decimal(18,2)")
      .HasComment("Monto de la transaccion.");

    builder.Property(p => p.CurrencyCode)
      .HasColumnType("char(3)")
      .HasComment("Heredado del Store.");

    builder.Property(p => p.ErrorCode)
      .HasMaxLength(100)
      .HasComment("Codigo de error del provider si fallo.");

    builder.Property(p => p.ErrorMessage)
      .HasMaxLength(1000)
      .HasComment("Mensaje de error del provider si fallo.");

    builder.Property(p => p.CreatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de creacion en UTC.");

    builder.Property(p => p.CreatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que creo el registro.");

    builder.Property(p => p.UpdatedAtUtc)
      .HasColumnType("datetime2")
      .HasComment("Fecha de ultima actualizacion en UTC.");

    builder.Property(p => p.UpdatedBy)
      .HasMaxLength(100)
      .HasComment("Usuario/actor que actualizo el registro.");

    builder.Property(p => p.RowVersion)
      .IsRowVersion()
      .HasComment("Token de concurrencia optimista.");

    builder.Property(p => p.Data)
      .HasColumnType("nvarchar(max)")
      .HasComment("JSON para extensiones no criticas.");

    builder.HasIndex(p => p.OrderId)
      .HasDatabaseName("IX_Payments_OrderId");

    builder.HasIndex(p => new { p.Provider, p.ProviderPaymentId })
      .HasDatabaseName("IX_Payments_Provider_ProviderPaymentId")
      .HasFilter("[ProviderPaymentId] IS NOT NULL");

    // Direccion real de la FK (ver nota en OrderConfiguration sobre por que Order.PaymentId no
    // tiene FK enforced: evita el ciclo).
    builder.HasOne<Order>()
      .WithMany()
      .HasForeignKey(p => p.OrderId)
      .HasConstraintName("FK_Payments_Orders_OrderId")
      .OnDelete(DeleteBehavior.Restrict);
  }
}
