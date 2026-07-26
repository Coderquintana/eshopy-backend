using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShopy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentEventsProcessed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PaymentEventsProcessed",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, comment: "Identificador unico del registro."),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, comment: "Provider de pago que emitio el evento."),
                    EventId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false, comment: "Id del evento segun el provider."),
                    ProcessedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, comment: "Fecha de procesamiento en UTC.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentEventsProcessed", x => x.Id);
                },
                comment: "Ledger de idempotencia de webhooks de pago. Global, no multi-tenant: (Provider, EventId) ya es unico a nivel provider.");

            migrationBuilder.CreateIndex(
                name: "UQ_PaymentEventsProcessed_Provider_EventId",
                table: "PaymentEventsProcessed",
                columns: new[] { "Provider", "EventId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentEventsProcessed");
        }
    }
}
