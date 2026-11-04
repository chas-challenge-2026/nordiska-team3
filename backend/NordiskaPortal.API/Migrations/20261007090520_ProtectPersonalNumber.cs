using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NordiskaPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class ProtectPersonalNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_PersonalNumber",
                table: "Users");

            migrationBuilder.AddColumn<string>(
                name: "PersonalNumberHash",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("d0000000-0000-0000-0000-000000000001"),
                column: "PersonalNumberHash",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonalNumberHash",
                table: "Users",
                column: "PersonalNumberHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_PersonalNumberHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PersonalNumberHash",
                table: "Users");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonalNumber",
                table: "Users",
                column: "PersonalNumber",
                unique: true);
        }
    }
}
