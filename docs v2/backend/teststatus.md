# Teststatus — backend

Mätt 2026-11-04 på `dev` (commit `547d2cf`).

- **420 tester** (xUnit): 401 godkända, 0 underkända och 19 hoppade över lokalt eftersom de kräver Postgres. Alla 420 körs i CI mot en Postgres 15.
- **Radtäckning 71,8 %** för API-projektet (utan genererade migrationer) och 80,5 % för Services.
- Demoflödet i [demo-data.md](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/docs/demo-data.md) provas manuellt. Skatterapportens kedja (generera, signera och verifiera med OpenSSL) har körts i Docker.

## Köra

Från rotmappen:

```bash
dotnet test backend/NordiskaPortal.Tests
```

Sätt `NORDISKA_TEST_DB` för att köra Postgres-testerna, se [local-development.md](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/docs/local-development.md), avsnitt 5. Lägg till `--collect:"XPlat Code Coverage"` för täckning.

## Vad som testas

| Område | Tester | Exempel |
|--------|--------|---------|
| Skatterapport och signering | 83 | Kö, underlag, signering, verifiering, nyckelskydd |
| FAQ | 80 | Sökning, rankning, stavfel |
| Konton och pengar | 79 | Överföring, belopp, idempotens, samtidighet |
| Ränta | 49 | Beräkning per dag, utbetalning, Riksbanken |
| Inloggning | 40 | PIN-låsning, refresh-token, JWT |
| Säkerhet och HTTP | 28 | Öppna endpoints, rate limiting, headers |
| Övrigt | 22 | Demodata, notiser, hälsokontroll |
| BankID | 20 | Start, status, QR-kod |
| Audit-logg | 19 | Hashkedja, verifiering |

## Täckning

Services 80,5 %, Data 99,8 %, Models 94,1 %, Middleware 93,8 % och DTOs 89,5 %. Lägre är Controllers 49,8 %, Validators 36,7 %, Extensions 27,2 %, Repositories 26,1 % (högre i CI, där Postgres-testerna körs) och `Program.cs` 0 %.

## CI och luckor

`ci.yml` kör vid push och pull request mot `dev`: backend (build och test mot Postgres 15) och en Docker-build. `infra-guard.yml` kontrollerar plattformskontraktet i [DRIFT.md](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/DRIFT.md) vid push till `develop` och `main`.

Täckningen mäts inte i CI, och skatterapportens kedja med de riktiga programmen testas bara manuellt. Fler brister: [kanda-brister.md](kanda-brister.md).
