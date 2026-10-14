using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NordiskaPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class TransactionIdempotencyKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Transactions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000004"),
                column: "IdempotencyKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000005"),
                column: "IdempotencyKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000006"),
                column: "IdempotencyKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000007"),
                column: "IdempotencyKey",
                value: null);

            migrationBuilder.UpdateData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000008"),
                column: "IdempotencyKey",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId_IdempotencyKey",
                table: "Transactions",
                columns: new[] { "AccountId", "IdempotencyKey" },
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Transactions_AccountId_IdempotencyKey",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Transactions");
        }
    }
}
