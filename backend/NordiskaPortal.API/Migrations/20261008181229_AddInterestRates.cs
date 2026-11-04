using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NordiskaPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddInterestRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountInterestPolicies",
                columns: table => new
                {
                    AccountType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    SpreadPercentagePoints = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountInterestPolicies", x => new { x.AccountType, x.EffectiveFrom });
                });

            migrationBuilder.CreateTable(
                name: "InterestRateObservations",
                columns: table => new
                {
                    SeriesId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: false),
                    FetchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InterestRateObservations", x => new { x.SeriesId, x.Date });
                });

            migrationBuilder.InsertData(
                table: "AccountInterestPolicies",
                columns: new[] { "AccountType", "EffectiveFrom", "SpreadPercentagePoints" },
                values: new object[,]
                {
                    { "CHECKING", new DateOnly(2026, 1, 1), -100m },
                    { "SAVINGS", new DateOnly(2026, 1, 1), -0.5m }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountInterestPolicies");

            migrationBuilder.DropTable(
                name: "InterestRateObservations");
        }
    }
}
