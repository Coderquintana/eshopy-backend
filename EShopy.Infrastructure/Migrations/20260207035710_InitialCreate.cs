using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShopy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del producto."),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador del tenant propietario."),
                    Slug = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, comment: "Slug publico del producto (SEO)."),
                    Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "SKU opcional del producto (normalizado a mayusculas)."),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre visible del producto."),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "Descripcion larga del producto."),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Precio unitario del producto."),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false, comment: "Codigo ISO 4217 de moneda."),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "Estado del producto (0=Draft,1=Active,2=Archived)."),
                    StockOnHand = table.Column<int>(type: "int", nullable: false, comment: "Stock simple disponible."),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha de creacion en UTC."),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha de ultima actualizacion en UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.CheckConstraint("CK_Products_Price_Positive", "[Price] >= 0");
                    table.CheckConstraint("CK_Products_Stock_NonNegative", "[StockOnHand] >= 0");
                },
                comment: "Catalogo de productos por tenant.");

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Name",
                table: "Products",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_TenantId_Status",
                table: "Products",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_Products_TenantId_Sku",
                table: "Products",
                columns: new[] { "TenantId", "Sku" },
                filter: "[Sku] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_Products_TenantId_Slug",
                table: "Products",
                columns: new[] { "TenantId", "Slug" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Products");
        }
    }
}
