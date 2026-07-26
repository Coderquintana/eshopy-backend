using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShopy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantsStoresSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Stores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del store."),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre publico de la tienda."),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false, comment: "Codigo ISO 4217 de moneda. Heredado por Products y Orders."),
                    Timezone = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Timezone IANA de la tienda, ej. 'America/Asuncion'."),
                    PrimaryColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true, comment: "Color primario de marca en hex, ej. '#FF5733'."),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true, comment: "URL del logo de la tienda."),
                    BackgroundColor = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true, comment: "Color de fondo en hex."),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Descripcion publica de la tienda."),
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
                    table.PrimaryKey("PK_Stores", x => x.Id);
                },
                comment: "Configuracion de tienda por tenant. 1:1 con Tenant en MVP.");

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico de la suscripcion."),
                    Plan = table.Column<byte>(type: "tinyint", nullable: false, comment: "Plan contratado (0=Basic,1=Gold,2=Diamond)."),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "Estado (0=PendingActivation,1=Active,2=PastDue,3=Suspended,4=Cancelled)."),
                    BillingCycleStart = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Inicio del ciclo de facturacion actual."),
                    BillingCycleEnd = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fin del ciclo de facturacion actual."),
                    PriceAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false, comment: "Precio del plan al momento de la suscripcion."),
                    CurrencyCode = table.Column<string>(type: "char(3)", nullable: false, comment: "Codigo ISO 4217 de moneda del cobro."),
                    ExternalSubscriptionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Id en la plataforma de billing externa (Fase 8)."),
                    CancelledAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Fecha de cancelacion, si aplica."),
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
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.CheckConstraint("CK_Subscriptions_PriceAmount_NonNegative", "[PriceAmount] >= 0");
                },
                comment: "Suscripcion mensual del tenant a un plan.");

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del tenant."),
                    Subdomain = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Subdominio unico en toda la plataforma."),
                    BusinessName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre legal del negocio."),
                    Status = table.Column<byte>(type: "tinyint", nullable: false, comment: "Estado del tenant (0=PendingPayment,1=Active,2=Suspended,3=Cancelled)."),
                    Plan = table.Column<byte>(type: "tinyint", nullable: false, comment: "Plan contratado (0=Basic,1=Gold,2=Diamond)."),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha de alta en UTC."),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Fecha de ultimo cambio de estado en UTC."),
                    ActivatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true, comment: "Fecha en que paso a Active por primera vez.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                },
                comment: "Tenants de la plataforma. Entidad global, no multi-tenant.");

            migrationBuilder.CreateTable(
                name: "TenantUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del usuario."),
                    KeycloakUserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Id del usuario en Keycloak."),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Email del usuario. Unico por tenant."),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Nombre visible del usuario."),
                    Role = table.Column<byte>(type: "tinyint", nullable: false, comment: "Rol del usuario (0=Owner,1=Admin,2=Staff)."),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, comment: "Permite deshabilitar el acceso sin eliminar el registro."),
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
                    table.PrimaryKey("PK_TenantUsers", x => x.Id);
                },
                comment: "Usuarios con acceso al panel de administracion de un tenant.");

            migrationBuilder.CreateIndex(
                name: "IX_Products_StoreId",
                table: "Products",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "UQ_Stores_TenantId",
                table: "Stores",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_Subscriptions_TenantId_NonCancelled",
                table: "Subscriptions",
                column: "TenantId",
                unique: true,
                filter: "[Status] <> 4");

            migrationBuilder.CreateIndex(
                name: "UQ_Tenants_Subdomain",
                table: "Tenants",
                column: "Subdomain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ_TenantUsers_TenantId_Email",
                table: "TenantUsers",
                columns: new[] { "TenantId", "Email" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Stores_StoreId",
                table: "Products",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Stores_StoreId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Tenants_TenantId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "Stores");

            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "TenantUsers");

            migrationBuilder.DropIndex(
                name: "IX_Products_StoreId",
                table: "Products");
        }
    }
}
