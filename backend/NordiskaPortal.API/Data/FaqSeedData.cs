using NordiskaPortal.API.Models;

namespace NordiskaPortal.API.Data;

public static class FaqSeedData
{
    private static readonly DateTime SeededAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static IReadOnlyList<FaqEntry> Entries { get; } = new[]
    {
        Entry(1, "Konto",
            "Hur öppnar jag ett nytt sparkonto?",
            "Gå till Dashboard och välj Skapa nytt sparkonto. Ge kontot ett namn och välj ikon och färg, så skapas det direkt.",
            "öppna konto, öppna sparkonto, öppna nytt konto, nytt konto, nytt sparkonto, skapa konto, skapa sparkonto, skapa nytt konto, starta sparkonto, ansöka konto"),

        Entry(2, "Konto",
            "Kan jag ha flera sparkonton?",
            "Ja, du kan skapa flera sparkonton och döpa dem efter vad du sparar till, till exempel Resekassa. Alla syns på din Dashboard.",
            "flera konton, flera sparkonton, fler konton, fler sparkonton, ett till konto, ytterligare konto, kontonamn, namnge konto, döpa konto, antal konton"),

        Entry(3, "Konto",
            "Hur avslutar jag mitt sparkonto?",
            "Ta ut hela saldot och kontakta sedan kundtjänst för att avsluta kontot.",
            "avsluta konto, avsluta sparkonto, stänga konto, stänga sparkonto, säga upp konto, säga upp sparkonto, ta bort konto, radera konto, lägga ner konto"),

        Entry(4, "Insättning",
            "Hur sätter jag in pengar?",
            "Välj Insättning/Uttag i menyn och sedan Sätt in. Ange konto och belopp och bekräfta. Pengarna syns direkt på kontot.",
            "insättning, sätta in, sätter in, sätt in, sätta in pengar, sätter in pengar, lägga in pengar, lägger in pengar, deponera, spara pengar"),

        Entry(5, "Uttag",
            "Hur tar jag ut pengar?",
            "Välj Insättning/Uttag i menyn och sedan Ta ut. Ange konto och belopp och bekräfta. Du kan bara ta ut pengar som finns på kontot, och saldot uppdateras direkt.",
            "uttag, ta ut, tar ut, ta ut pengar, tar ut pengar, ta ut saldo, ta ut belopp, plocka ut pengar, hämta ut pengar, göra uttag, ta pengar"),

        Entry(6, "Uttag",
            "Hur lång tid tar ett uttag?",
            "Ett uttag registreras direkt och saldot på kontot uppdateras med en gång.",
            "tid uttag, lång tid uttag, snabbt uttag, uttaget syns, när kommer pengarna, dröja uttag, handläggningstid"),

        Entry(7, "Överföring",
            "Hur flyttar jag pengar mellan mina konton?",
            "Välj Insättning/Uttag i menyn och sedan Mellan konton. Välj kontot pengarna ska flyttas från och till, ange belopp och bekräfta.",
            "flytta pengar, flytta mellan konton, mellan konton, överföring, överföra, överföra pengar, föra över pengar, flytta saldo, från konto till konto"),

        Entry(8, "Ränta",
            "När betalas räntan ut?",
            "Räntan beräknas dagligen och betalas ut den 31 december varje år.",
            "ränta, räntan, ränteutbetalning, utbetalning ränta, räntesats, ränta procent, få ränta, när betalas ränta, ränta 31 december, räkna ränta"),

        Entry(9, "Rapporter",
            "Var hittar jag mitt räntebesked eller årsbesked?",
            "Välj Skatterapport i menyn och sedan år. Där finns underlaget för ränta och transaktioner för det året.",
            "räntebesked, årsbesked, skatterapport, skatteunderlag, skatt, deklaration, deklarera, kontrolluppgift, underlag, ladda ner rapport"),

        Entry(10, "Rapporter",
            "Hur får jag ett kontoutdrag?",
            "Under Historik i menyn ser du alla dina transaktioner och kan filtrera på konto och typ. Ett fullständigt underlag för hela året finns i Skatterapport.",
            "kontoutdrag, utdrag, transaktioner, transaktionshistorik, historik, se transaktioner, mina transaktioner, kontohistorik, händelser"),

        Entry(11, "Trygghet",
            "Är mina pengar skyddade?",
            "Ja, dina pengar omfattas av insättningsgarantin enligt de villkor som gäller för kontot.",
            "skyddade, skyddad, insättningsgaranti, garanti, trygga pengar, säkra pengar, säkerhet, riksgälden, bankkonkurs, förlora pengar"),

        Entry(12, "Villkor",
            "Var hittar jag villkoren för mitt sparkonto?",
            "Fullständiga avtalsvillkor skickas till dig per post. Kontakta kundtjänst om du saknar dem.",
            "villkor, villkoren, avtal, avtalsvillkor, kundavtal, regler, bestämmelser, kontovillkor"),

        Entry(13, "Profil",
            "Hur ändrar jag mina kontaktuppgifter?",
            "Klicka på din profil uppe till höger och välj Profil och inställningar för att ändra namn och e-post.",
            "kontaktuppgifter, ändra e-post, byta e-post, ändra epost, e-postadress, mejl, mail, ändra namn, byta namn, profil, inställningar, uppgifter"),

        Entry(14, "Kontakt",
            "Hur kontaktar jag kundtjänst?",
            "Ring 08-123 456 78 vardagar 8-18 eller mejla hej@nordiska.se.",
            "kundtjänst, kundservice, kontakta, kontakt, ringa, telefon, telefonnummer, ring oss, mejla, support, öppettider, chatt, prata med någon"),
    };

    private static FaqEntry Entry(int number, string category, string question, string answer, string keywords) => new()
    {
        Id = Guid.Parse($"fa000000-0000-0000-0000-{number:D12}"),
        Category = category,
        Question = question,
        Answer = answer,
        Keywords = keywords,
        CreatedAt = SeededAt
    };
}
