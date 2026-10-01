using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NordiskaPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDemoSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FirstName", "LastName", "PersonalNumber", "PinHash", "RefreshToken", "RefreshTokenExpiryTime", "UpdatedAt" },
                values: new object[] { new Guid("d0000000-0000-0000-0000-000000000001"), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "demo@nordiskaportal.se", "Demo", "Kund", "19850615-5678", "$2b$12$Ma9ikA7xtUOXMH86.OZA7eYIEb.yDiazDkk5uu5M/4PpbuC3ORvSu", null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "AccountNumber", "AccountType", "CreatedAt", "Name", "Status", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { new Guid("d0000000-0000-0000-0000-000000000002"), "NKM-10001", "CHECKING", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Transaktionskonto", "ACTIVE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d0000000-0000-0000-0000-000000000001") },
                    { new Guid("d0000000-0000-0000-0000-000000000003"), "NKM-10002", "SAVINGS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sparkonto", "ACTIVE", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new Guid("d0000000-0000-0000-0000-000000000001") }
                });

            migrationBuilder.InsertData(
                table: "Transactions",
                columns: new[] { "Id", "AccountId", "Amount", "CompletedAt", "CreatedAt", "Status", "TransactionType" },
                values: new object[,]
                {
                    { new Guid("d0000000-0000-0000-0000-000000000004"), new Guid("d0000000-0000-0000-0000-000000000002"), 5000m, new DateTime(2026, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), "COMPLETED", "DEPOSIT" },
                    { new Guid("d0000000-0000-0000-0000-000000000005"), new Guid("d0000000-0000-0000-0000-000000000002"), 3000m, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "COMPLETED", "DEPOSIT" },
                    { new Guid("d0000000-0000-0000-0000-000000000006"), new Guid("d0000000-0000-0000-0000-000000000002"), 750m, new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), "COMPLETED", "WITHDRAWAL" },
                    { new Guid("d0000000-0000-0000-0000-000000000007"), new Guid("d0000000-0000-0000-0000-000000000003"), 20000m, new DateTime(2026, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "COMPLETED", "DEPOSIT" },
                    { new Guid("d0000000-0000-0000-0000-000000000008"), new Guid("d0000000-0000-0000-0000-000000000003"), 5000m, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "COMPLETED", "DEPOSIT" }
                });

            migrationBuilder.InsertData(
                table: "LedgerEntries",
                columns: new[] { "Id", "AccountId", "Amount", "CreatedAt", "Description", "EntryType", "TransactionId" },
                values: new object[,]
                {
                    { new Guid("d0000000-0000-0000-0000-000000000009"), new Guid("d0000000-0000-0000-0000-000000000002"), 5000m, new DateTime(2026, 1, 2, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit", "DEPOSIT", new Guid("d0000000-0000-0000-0000-000000000004") },
                    { new Guid("d0000000-0000-0000-0000-00000000000a"), new Guid("d0000000-0000-0000-0000-000000000002"), 3000m, new DateTime(2026, 1, 10, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit", "DEPOSIT", new Guid("d0000000-0000-0000-0000-000000000005") },
                    { new Guid("d0000000-0000-0000-0000-00000000000b"), new Guid("d0000000-0000-0000-0000-000000000002"), -750m, new DateTime(2026, 1, 20, 0, 0, 0, 0, DateTimeKind.Utc), "Withdrawal", "WITHDRAWAL", new Guid("d0000000-0000-0000-0000-000000000006") },
                    { new Guid("d0000000-0000-0000-0000-00000000000c"), new Guid("d0000000-0000-0000-0000-000000000003"), 20000m, new DateTime(2026, 1, 3, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit", "DEPOSIT", new Guid("d0000000-0000-0000-0000-000000000007") },
                    { new Guid("d0000000-0000-0000-0000-00000000000d"), new Guid("d0000000-0000-0000-0000-000000000003"), 5000m, new DateTime(2026, 1, 15, 0, 0, 0, 0, DateTimeKind.Utc), "Deposit", "DEPOSIT", new Guid("d0000000-0000-0000-0000-000000000008") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "LedgerEntries",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "LedgerEntries",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-00000000000a"));

            migrationBuilder.DeleteData(
                table: "LedgerEntries",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-00000000000b"));

            migrationBuilder.DeleteData(
                table: "LedgerEntries",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-00000000000c"));

            migrationBuilder.DeleteData(
                table: "LedgerEntries",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-00000000000d"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "Transactions",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "Accounts",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000001"));
        }
    }
}
