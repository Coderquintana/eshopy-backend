using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShopy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreProfileCustomization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Stores",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "Direccion publica de la tienda en texto libre.");

            migrationBuilder.AddColumn<string>(
                name: "BusinessHours",
                table: "Stores",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "Horario publico de atencion en texto libre.");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Stores",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: true,
                comment: "Email de contacto publico de la tienda, independiente del owner.");

            migrationBuilder.AddColumn<string>(
                name: "ContactWhatsapp",
                table: "Stores",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: true,
                comment: "Numero de WhatsApp publico de la tienda.");

            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "Stores",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "URL absoluta del perfil de Facebook de la tienda.");

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "Stores",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "URL absoluta del perfil de Instagram de la tienda.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "BusinessHours",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "ContactWhatsapp",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "Stores");
        }
    }
}
