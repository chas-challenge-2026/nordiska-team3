using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NordiskaPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFaqEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000002"),
                column: "Keywords",
                value: "flera konton, flera sparkonton, fler konton, fler sparkonton, ytterligare konto, kontonamn, namnge konto, döpa konto, antal konton");

            migrationBuilder.UpdateData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000003"),
                column: "Keywords",
                value: "avsluta konto, avsluta sparkonto, stänga konto, stänga sparkonto, säga upp konto, säga upp sparkonto, ta bort konto, ta bort sparkonto, radera konto, radera sparkonto, lägga ner konto");

            migrationBuilder.UpdateData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000007"),
                column: "Keywords",
                value: "flytta pengar, flytta mellan konton, mellan konton, överföring, överföra, överföra pengar, föra över pengar, flytta saldo, eget konto, egna konton");

            migrationBuilder.InsertData(
                table: "FaqEntries",
                columns: new[] { "Id", "Answer", "Category", "CreatedAt", "Keywords", "Question" },
                values: new object[,]
                {
                    { new Guid("fa000000-0000-0000-0000-000000000015"), "Välj Insättning/Uttag i menyn och sedan Mellan konton. Välj Annat konto under Mottagare och skriv mottagarens kontonummer, till exempel NKM-12345. Ange belopp och bekräfta.", "Överföring", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "någon annan, annan person, annat konto, annans konto, skicka pengar, skicka till, föra över till annan, mottagare, mottagarens kontonummer", "Hur för jag över pengar till någon annan?" },
                    { new Guid("fa000000-0000-0000-0000-000000000016"), "Av säkerhetsskäl loggas du ut automatiskt efter 15 minuter utan aktivitet. Du behöver också logga in igen om du har stängt fliken eller öppnar portalen i en ny flik.", "Inloggning", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "utloggad, loggades ut, loggas ut, utloggning, inaktivitet, inaktiv, kastades ut, sessionen gick ut, logga in igen, ny flik", "Varför blev jag utloggad?" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000016"));

            migrationBuilder.UpdateData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000002"),
                column: "Keywords",
                value: "flera konton, flera sparkonton, fler konton, fler sparkonton, ett till konto, ytterligare konto, kontonamn, namnge konto, döpa konto, antal konton");

            migrationBuilder.UpdateData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000003"),
                column: "Keywords",
                value: "avsluta konto, avsluta sparkonto, stänga konto, stänga sparkonto, säga upp konto, säga upp sparkonto, ta bort konto, radera konto, lägga ner konto");

            migrationBuilder.UpdateData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000007"),
                column: "Keywords",
                value: "flytta pengar, flytta mellan konton, mellan konton, överföring, överföra, överföra pengar, föra över pengar, flytta saldo, från konto till konto");
        }
    }
}
