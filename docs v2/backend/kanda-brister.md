# Kända brister och avgränsningar — backend

Läget gäller `dev` 2026-11-04.

## Avgränsningar

- **Riktig BankID:** kräver avtal och certifikat. Vi har `Off` (standard och stage), `Mock` (bara Development) och `Test`.
- **Kontohistorik som PDF:** inte byggt. Det kräver en endpoint i backend och en ny dokumenttyp i PDF-generatorn.
- **Ränta:** betalas ut 1 januari (första gången 2027-01-01). Räntan hämtas från Riksbanken, och når servern den inte visas ingen ränta.
- **Skatt:** förenklad, 30 % av ränteintäkterna. Ett underlag och ingen deklaration.
- **E-post:** kö och retry finns men utskicket simuleras (loggas).
- **Utanför MVP:** avsluta konto, ändra uppgifter eller PIN, glömd PIN och uttagsgränser.
- **En instans:** BankID-ordrar, rate limiting och skatterapportkön ligger i minnet.
- **Stage och prod:** bara demodata.

## Kända brister

- Täckningen mäts inte i CI (71,8 % lokalt), och skatterapportens kedja testas bara manuellt. Se [teststatus.md](teststatus.md).
- Docker-bilden har Development som standard och körs som root. Compose sätter `Production` för stage och prod.
- Nycklarna i `api_secrets` säkerhetskopieras inte automatiskt. Försvinner de vägrar API:et starta, se [local-development.md](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/docs/local-development.md), avsnitt 3.
- Rapporternas PDF-filer ligger på serverns disk (volymen `tax_reports`).
- Två genererade JSON-filer ligger kvar i `backend/NordiskaPortal.API/tax-reports/` (#375).
- v1-projektet `backend/NordiskaPortal` ligger kvar i repot och används inte.
