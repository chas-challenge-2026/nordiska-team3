using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NordiskaPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class AddFaqEntriesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaqEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Question = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Answer = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Keywords = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqEntries", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "FaqEntries",
                columns: new[] { "Id", "Answer", "Category", "CreatedAt", "Keywords", "Question" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "You can apply for a new account directly through our portal under the Accounts tab.", "Accounts", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "account, open, apply, create", "How do I open a new account?" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Our current savings account interest rate is 3.5% annually.", "Savings", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "interest, rate, savings, deposit", "What is the interest rate on the savings account?" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaqEntries");
        }
    }
}
