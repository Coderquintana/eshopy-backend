using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShopy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersPaymentsTenantCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del pedido."),
                    StoreId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Store al que pertenece el pedido."),
                    OrderNumber = table.Column<int>(type: "int", nullable: false, comment: "Secuencial por tenant. Asignado atomicamente por ICheckoutWriter, no en la creacion."),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "Estado del pedido (0=PendingPayment,1=Paid,2=Cancelled,3=Refunded)."),
                    BuyerEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Email del comprador al momento del checkout."),
                    BuyerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre del comprador al momento del checkout."),
                    ShippingAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Direccion de entrega."),
                    CartToken = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "CartToken del carrito origen."),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false, comment: "Heredado del Store al momento del checkout."),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Suma de OrderItems, calculado al crear."),
                    PaymentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Referencia al Payment activo. Sin FK enforced (ver Payment.OrderId)."),
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
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.CheckConstraint("CK_Orders_TotalAmount_NonNegative", "[TotalAmount] >= 0");
                    table.ForeignKey(
                        name: "FK_Orders_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Pedido generado desde checkout.");

            migrationBuilder.CreateTable(
                name: "TenantCounters",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador del tenant propietario."),
                    CounterType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Tipo de contador (ej. 'OrderNumber')."),
                    CurrentValue = table.Column<int>(type: "int", nullable: false, comment: "Valor actual del contador. Concurrency token EF — sin SQL crudo.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantCounters", x => new { x.TenantId, x.CounterType });
                },
                comment: "Contadores atomicos por tenant (ej. secuencia de OrderNumber).");

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del item."),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Pedido al que pertenece."),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Producto referenciado (FK a Products). Referencia historica, no se borra si el producto se archiva."),
                    ProductName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false, comment: "Snapshot del nombre al momento del checkout."),
                    ProductSku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true, comment: "Snapshot del SKU al momento del checkout."),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Snapshot del precio unitario al momento del checkout."),
                    Quantity = table.Column<int>(type: "int", nullable: false, comment: "Cantidad del item.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.CheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] >= 1");
                    table.CheckConstraint("CK_OrderItems_UnitPrice_NonNegative", "[UnitPrice] >= 0");
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Snapshot inmutable de un producto al momento del checkout.");

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del pago."),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Pedido al que pertenece (FK a Orders)."),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "Estado del pago (0=Initiated,1=Authorized,2=Captured,3=Failed,4=Refunded)."),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Provider de pago (ej. 'bancard', 'pagopar', 'fake')."),
                    ProviderPaymentId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Id de la transaccion en el provider."),
                    ProviderPaymentUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "URL de pago devuelta al frontend."),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Monto de la transaccion."),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false, comment: "Heredado del Store."),
                    ErrorCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Codigo de error del provider si fallo."),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Mensaje de error del provider si fallo."),
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
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.CheckConstraint("CK_Payments_Amount_NonNegative", "[Amount] >= 0");
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Intento de pago asociado a un Order.");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StoreId",
                table: "Orders",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_BuyerEmail",
                table: "Orders",
                columns: new[] { "TenantId", "BuyerEmail" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_TenantId_Status",
                table: "Orders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_Orders_TenantId_OrderNumber",
                table: "Orders",
                columns: new[] { "TenantId", "OrderNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Provider_ProviderPaymentId",
                table: "Payments",
                columns: new[] { "Provider", "ProviderPaymentId" },
                filter: "[ProviderPaymentId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "TenantCounters");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
