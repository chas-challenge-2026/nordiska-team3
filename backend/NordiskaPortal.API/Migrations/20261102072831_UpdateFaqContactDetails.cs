using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NordiskaPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFaqContactDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000014"),
                column: "Answer",
                value: "Ring 08-23 28 00 vardagar 8:30-16:30 eller mejla info@nordiska.se.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000014"),
                column: "Answer",
                value: "Ring 08-123 456 78 vardagar 8-18 eller mejla hej@nordiska.se.");
        }
    }
}
