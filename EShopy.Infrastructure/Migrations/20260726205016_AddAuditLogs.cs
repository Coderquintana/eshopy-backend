using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShopy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del registro."),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true, comment: "Tenant afectado por la accion. Nullable: hay acciones a nivel plataforma (ej. activacion de tenant via SUPERADMIN)."),
                    UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true, comment: "Id del usuario autenticado que ejecuto la accion. Null si no hay usuario autenticado (ej. webhook)."),
                    UserEmail = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true, comment: "Email del usuario autenticado. Null si no hay usuario autenticado."),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Modulo.Operacion, ej. 'Order.ChangeStatus'."),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, comment: "Tipo de entidad afectada, ej. 'Order'."),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Id de la entidad afectada."),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true, comment: "Texto libre con el detalle de la accion (ej. cambio de estado). No es JSON estructurado a proposito — YAGNI hasta que un caso de uso real lo pida."),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha de la accion en UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                },
                comment: "Registro append-only de operaciones sensibles (F9-03). Nunca se actualiza ni se borra.");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EntityType_EntityId",
                table: "AuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_CreatedAtUtc",
                table: "AuditLogs",
                columns: new[] { "TenantId", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");
        }
    }
}
