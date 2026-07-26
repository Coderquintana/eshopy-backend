using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShopy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCartsCartItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Carts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del carrito."),
                    CartToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "UUID generado en el frontend."),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Se extiende con cada actividad. Para limpieza de carritos abandonados (F6-04)."),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador del tenant propietario."),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha de creacion en UTC."),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Usuario/actor que creo el registro."),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Fecha de ultima actualizacion en UTC."),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Usuario/actor que actualizo el registro."),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true, comment: "Token de concurrencia optimista."),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true, comment: "JSON para extensiones no criticas.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Carts", x => x.Id);
                },
                comment: "Carrito server-side, previo al checkout.");

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del item."),
                    CartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Carrito al que pertenece."),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Producto referenciado (FK a Products)."),
                    Quantity = table.Column<int>(type: "int", nullable: false, comment: "Cantidad. Acumula si el producto se agrega de nuevo, no duplica fila."),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha de creacion en UTC."),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Fecha de ultima actualizacion en UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.CheckConstraint("CK_CartItems_Quantity_Positive", "[Quantity] >= 1");
                    table.ForeignKey(
                        name: "FK_CartItems_Carts_CartId",
                        column: x => x.CartId,
                        principalTable: "Carts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Items de un carrito. Un producto, una fila — sin snapshot de precio.");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "UQ_CartItems_CartId_ProductId",
                table: "CartItems",
                columns: new[] { "CartId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Carts_TenantId_CartToken",
                table: "Carts",
                columns: new[] { "TenantId", "CartToken" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "Carts");
        }
    }
}
