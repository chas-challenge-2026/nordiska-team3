# Demodata

Skapas av `DemoDataSeeder` vid start när `Demo__SeedData` är true (lokalt via `docker-compose.override.yml`, på stage via `docker-compose.yml`). Körs om utan att göra något om datan redan finns. Alla kunder har PIN 1234 och påhittade personnummer.

| Kund | Personnummer | Konton |
|------|--------------|--------|
| Anna Lindqvist (huvudkund) | 19880312-4421 | NKM-20001 Transaktionskonto, NKM-20002 Sparkonto, NKM-20003 Semesterkassan |
| Erik Johansson | 19810927-1136 | NKM-30001 Transaktionskonto, NKM-30002 Sparkonto |
| Sara Nilsson | 19930504-7782 | NKM-40001 Transaktionskonto, NKM-40002 Sparkonto, NKM-40003 Buffert |
| Omar Hassan | 19790218-3355 | NKM-50001 Transaktionskonto, NKM-50002 Sparkonto |
| Demo Kund (från migrationen) | 19850615-5678 | NKM-10001, NKM-10002 |

Historiken går från januari 2025 till dagens datum: månadslön, hyra och kontantuttag, månadssparande till Sparkontot, några överföringar mellan egna konton och några mellan kunderna.

Räntan för 2025 är bokförd på sparkontona den 31 december 2025. Den är beräknad dag för dag med appens egen `InterestCalculator` på Riksbankens styrränta 2025 (2,50 / 2,25 från 5 feb / 2,00 från 25 juni / 1,75 från 1 okt) minus 0,5 procentenheter. För Anna: Sparkonto 1 794,36 kr, och skatterapporten för 2025 visar ränteintäkt 1 794,36 och kapitalskatt 538,31.

## Demomanus

1. Logga in som Anna. Visa de tre kontona och "Upplupen ränta i år" på Sparkontot (2026).
2. Öppna historiken och visa insättningar, uttag och överföringar (egna konton visar kontonamn, andra kunder visar maskerat namn, till exempel "Erik J.").
3. Gör en insättning och ett uttag.
4. Flytta pengar mellan Annas egna konton (till exempel NKM-20002 till NKM-20003).
5. Skicka pengar till en annan kund: skriv NKM-30001 (Erik) som mottagare.
6. Skapa skatterapporten för 2025 och ladda ner PDF:en: hela årets historik, ränta och kapitalskatt.
7. Skapa skatterapporten för 2026: historiken hittills, ränta 0,00 eftersom året inte är slut.

## Begränsningar

- Räntan för 2025 är inlagd av demodatan, inte utbetald av ränte-jobbet. Det riktiga jobbet bokför ränta först vid årsskiftet 2026/2027 (`Interest:FirstPayoutYear` är 2026).
- Skatterapporten för pågående år visar ränta 0,00. En rapport med ränta hittills kräver en ändring i native-koden och är parkerad.
- Upplupen ränta i appen (2026) kräver att API:et kan hämta styrräntan från api.riksbank.se.
- Historiken är inlagd som rader direkt i databasen. Den har inga notiser och ingen audit-logg.