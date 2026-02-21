using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EShopy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreIdToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "Products",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "Store al que pertenece el producto (FK a Stores).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Products");
        }
    }
}
