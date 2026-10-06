using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace NordiskaPortal.API.Migrations
{
    /// <inheritdoc />
    public partial class SeedSwedishFaqEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.InsertData(
                table: "FaqEntries",
                columns: new[] { "Id", "Answer", "Category", "CreatedAt", "Keywords", "Question" },
                values: new object[,]
                {
                    { new Guid("fa000000-0000-0000-0000-000000000001"), "Gå till Dashboard och välj Skapa nytt sparkonto. Ge kontot ett namn och välj ikon och färg, så skapas det direkt.", "Konto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "öppna konto, öppna sparkonto, öppna nytt konto, nytt konto, nytt sparkonto, skapa konto, skapa sparkonto, skapa nytt konto, starta sparkonto, ansöka konto", "Hur öppnar jag ett nytt sparkonto?" },
                    { new Guid("fa000000-0000-0000-0000-000000000002"), "Ja, du kan skapa flera sparkonton och döpa dem efter vad du sparar till, till exempel Resekassa. Alla syns på din Dashboard.", "Konto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "flera konton, flera sparkonton, fler konton, fler sparkonton, ett till konto, ytterligare konto, kontonamn, namnge konto, döpa konto, antal konton", "Kan jag ha flera sparkonton?" },
                    { new Guid("fa000000-0000-0000-0000-000000000003"), "Ta ut hela saldot och kontakta sedan kundtjänst för att avsluta kontot.", "Konto", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "avsluta konto, avsluta sparkonto, stänga konto, stänga sparkonto, säga upp konto, säga upp sparkonto, ta bort konto, radera konto, lägga ner konto", "Hur avslutar jag mitt sparkonto?" },
                    { new Guid("fa000000-0000-0000-0000-000000000004"), "Välj Insättning/Uttag i menyn och sedan Sätt in. Ange konto och belopp och bekräfta. Pengarna syns direkt på kontot.", "Insättning", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "insättning, sätta in, sätter in, sätt in, sätta in pengar, sätter in pengar, lägga in pengar, lägger in pengar, deponera, spara pengar", "Hur sätter jag in pengar?" },
                    { new Guid("fa000000-0000-0000-0000-000000000005"), "Välj Insättning/Uttag i menyn och sedan Ta ut. Ange konto och belopp och bekräfta. Du kan bara ta ut pengar som finns på kontot, och saldot uppdateras direkt.", "Uttag", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "uttag, ta ut, tar ut, ta ut pengar, tar ut pengar, ta ut saldo, ta ut belopp, plocka ut pengar, hämta ut pengar, göra uttag, ta pengar", "Hur tar jag ut pengar?" },
                    { new Guid("fa000000-0000-0000-0000-000000000006"), "Ett uttag registreras direkt och saldot på kontot uppdateras med en gång.", "Uttag", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "tid uttag, lång tid uttag, snabbt uttag, uttaget syns, när kommer pengarna, dröja uttag, handläggningstid", "Hur lång tid tar ett uttag?" },
                    { new Guid("fa000000-0000-0000-0000-000000000007"), "Välj Insättning/Uttag i menyn och sedan Mellan konton. Välj kontot pengarna ska flyttas från och till, ange belopp och bekräfta.", "Överföring", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "flytta pengar, flytta mellan konton, mellan konton, överföring, överföra, överföra pengar, föra över pengar, flytta saldo, från konto till konto", "Hur flyttar jag pengar mellan mina konton?" },
                    { new Guid("fa000000-0000-0000-0000-000000000008"), "Räntan beräknas dagligen och betalas ut den 31 december varje år.", "Ränta", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ränta, räntan, ränteutbetalning, utbetalning ränta, räntesats, ränta procent, få ränta, när betalas ränta, ränta 31 december, räkna ränta", "När betalas räntan ut?" },
                    { new Guid("fa000000-0000-0000-0000-000000000009"), "Välj Skatterapport i menyn och sedan år. Där finns underlaget för ränta och transaktioner för det året.", "Rapporter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "räntebesked, årsbesked, skatterapport, skatteunderlag, skatt, deklaration, deklarera, kontrolluppgift, underlag, ladda ner rapport", "Var hittar jag mitt räntebesked eller årsbesked?" },
                    { new Guid("fa000000-0000-0000-0000-000000000010"), "Under Historik i menyn ser du alla dina transaktioner och kan filtrera på konto och typ. Ett fullständigt underlag för hela året finns i Skatterapport.", "Rapporter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "kontoutdrag, utdrag, transaktioner, transaktionshistorik, historik, se transaktioner, mina transaktioner, kontohistorik, händelser", "Hur får jag ett kontoutdrag?" },
                    { new Guid("fa000000-0000-0000-0000-000000000011"), "Ja, dina pengar omfattas av insättningsgarantin enligt de villkor som gäller för kontot.", "Trygghet", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "skyddade, skyddad, insättningsgaranti, garanti, trygga pengar, säkra pengar, säkerhet, riksgälden, bankkonkurs, förlora pengar", "Är mina pengar skyddade?" },
                    { new Guid("fa000000-0000-0000-0000-000000000012"), "Fullständiga avtalsvillkor skickas till dig per post. Kontakta kundtjänst om du saknar dem.", "Villkor", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "villkor, villkoren, avtal, avtalsvillkor, kundavtal, regler, bestämmelser, kontovillkor", "Var hittar jag villkoren för mitt sparkonto?" },
                    { new Guid("fa000000-0000-0000-0000-000000000013"), "Klicka på din profil uppe till höger och välj Profil och inställningar för att ändra namn och e-post.", "Profil", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "kontaktuppgifter, ändra e-post, byta e-post, ändra epost, e-postadress, mejl, mail, ändra namn, byta namn, profil, inställningar, uppgifter", "Hur ändrar jag mina kontaktuppgifter?" },
                    { new Guid("fa000000-0000-0000-0000-000000000014"), "Ring 08-123 456 78 vardagar 8-18 eller mejla hej@nordiska.se.", "Kontakt", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "kundtjänst, kundservice, kontakta, kontakt, ringa, telefon, telefonnummer, ring oss, mejla, support, öppettider, chatt, prata med någon", "Hur kontaktar jag kundtjänst?" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000001"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000002"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000003"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000004"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000005"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000006"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000007"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000008"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000009"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000010"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "FaqEntries",
                keyColumn: "Id",
                keyValue: new Guid("fa000000-0000-0000-0000-000000000014"));

            migrationBuilder.InsertData(
                table: "FaqEntries",
                columns: new[] { "Id", "Answer", "Category", "CreatedAt", "Keywords", "Question" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), "You can apply for a new account directly through our portal under the Accounts tab.", "Accounts", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "account, open, apply, create", "How do I open a new account?" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), "Our current savings account interest rate is 3.5% annually.", "Savings", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "interest, rate, savings, deposit", "What is the interest rate on the savings account?" }
                });
        }
    }
}
