using EShopy.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EShopy.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
  public void Configure(EntityTypeBuilder<Product> builder)
  {
    builder.ToTable("Products", table =>
    {
      table.HasComment("Catalogo de productos por tenant.");
      table.HasCheckConstraint("CK_Products_Price_Positive", "[Price] >= 0");
      table.HasCheckConstraint("CK_Products_Stock_NonNegative", "[StockOnHand] >= 0");
    });

    builder.HasKey(p => p.Id).HasName("PK_Products");

    builder.Property(p => p.Id)
      .HasComment("Identificador unico del producto.");

    builder.Property(p => p.TenantId)
      .HasComment("Identificador del tenant propietario.");

    builder.Property(p => p.Sku)
      .HasMaxLength(64)
      .HasComment("SKU opcional del producto (normalizado a mayusculas).");

    builder.Property(p => p.Slug)
      .HasMaxLength(128)
      .HasComment("Slug publico del producto (SEO).");

    builder.Property(p => p.Name)
      .HasMaxLength(200)
      .HasComment("Nombre visible del producto.");

    builder.Property(p => p.Description)
      .HasComment("Descripcion larga del producto.");

    builder.Property(p => p.Price)
      .HasColumnType("decimal(18,2)")
      .HasComment("Precio unitario del producto.");

    builder.Property(p => p.CurrencyCode)
      .HasColumnType("char(3)")
      .HasComment("Codigo ISO 4217 de moneda.");

    builder.Property(p => p.Status)
      .HasConversion<byte>()
      .HasColumnType("tinyint")
      .HasComment("Estado del producto (0=Draft,1=Active,2=Archived).");

    builder.Property(p => p.StockOnHand)
      .HasComment("Stock simple disponible.");

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

    builder.HasIndex(p => new { p.TenantId, p.Slug })
      .IsUnique()
      .HasDatabaseName("UQ_Products_TenantId_Slug");

    builder.HasIndex(p => new { p.TenantId, p.Sku })
      .HasDatabaseName("UQ_Products_TenantId_Sku")
      .HasFilter("[Sku] IS NOT NULL");

    builder.HasIndex(p => new { p.TenantId, p.Status })
      .HasDatabaseName("IX_Products_TenantId_Status");

    builder.HasIndex(p => new { p.TenantId, p.Name })
      .HasDatabaseName("IX_Products_TenantId_Name");
  }
}
