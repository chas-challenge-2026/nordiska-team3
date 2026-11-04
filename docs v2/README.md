# Nordiska Sparportal

## Backend

API:et bakom sparportalen: inloggning med PIN eller BankID, konton, insättning, uttag och överföring, ränta, skatterapport som signerad PDF och FAQ. Byggt i .NET 8, Entity Framework Core och PostgreSQL 15. Skatterapporterna skapas av två inbyggda program i `native/` som API:et startar.

### Kom igång

Du behöver Git och Docker Desktop (igång).

```bash
git clone https://github.com/chas-challenge-2026/nordiska-team3.git
cd nordiska-team3/infra
docker compose up --build
```

Öppna `http://localhost:5077` (API och frontend). Portarna 5077 och 5432 måste vara lediga. Databas och nycklar skapas automatiskt, och lokalt läggs demodata in.

- Stoppa med `docker compose down`. Börja om med `docker compose down -v` (tar bort databas och nycklar tillsammans).
- Swagger finns på `/swagger` och hälsokontroll på `/health`.
- Logga in som en demokund i [docs/demo-data.md](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/docs/demo-data.md), PIN `1234`. I Swagger: kör `POST /api/auth/login-pin`, kopiera `accessToken` och klistra in den under **Authorize** utan ordet "Bearer".
- Utan Docker: se [docs/local-development.md](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/docs/local-development.md).

### Tester

Från rotmappen:

```bash
dotnet test backend/NordiskaPortal.Tests
```

De 19 testerna som kräver Postgres hoppas över om `NORDISKA_TEST_DB` inte är satt, och körs alltid i CI. Se [teststatus](backend/teststatus.md).

### Struktur

`NordiskaPortal.API/` har Controllers, Services (affärslogik), Repositories, Data (DbContext och seed), Migrations, Models, DTOs, Validators, Middleware och Extensions. `NordiskaPortal.Tests/` har xUnit-testerna. `NordiskaPortal/` är v1 och används inte.

### API

Alla endpoints finns i Swagger. I korthet: `/api/auth` (register, login-pin, refresh, me, logout), `/api/auth/bankid`, `/api/accounts` (konton, saldo, deposit, withdraw, transfer, transaktioner), `/api/transactions`, `/api/taxreports` (skapa, lista, PDF, signatur, publik nyckel), `/api/faq` och `/api/health`. Allt kräver inloggning utom register, login-pin, refresh, BankID och health.

### Så är det byggt

- **Pengar:** saldot räknas ur en huvudbok (`LedgerEntry`), med radlås och en transaktion per order. Insättning, uttag och överföring tar en valfri `Idempotency-Key`.
- **Inloggning:** PIN med BCrypt, spärr efter fem fel (HTTP 423), JWT (15 minuter) och refresh-token i en HttpOnly-cookie (30 minuter). Personnummer krypteras och maskas.
- **Skydd:** rate limiting, säkerhetsheaders och en audit-logg med hashkedja.
- **Ränta:** Riksbankens styrränta plus påslag per kontotyp. Upplupen ränta visas på kontot och betalas ut 1 januari.
- **Skatterapport:** skapas i en kö av `pdf_generator`, signeras av `pdf_signer` och blir `READY` först när signaturen är verifierad. Kontrollera den med `openssl dgst -sha256 -verify <publik nyckel> -signature <fil>.pdf.sig <fil>.pdf`.

### Avgränsningar

- BankID är mockat lokalt (`BankId__Mode=Mock`), kan köras mot BankIDs testmiljö (`Test`) och är avstängt på stage (`Off`).
- Räntan betalas ut en gång om året, den 1 januari.
- Skatterapporter fungerar bara i Docker.

### Mer information

[Beslutslogg](backend/beslutslogg.md), [teststatus](backend/teststatus.md), [kända brister](backend/kanda-brister.md), [fallback](backend/fallback.md), [feed forward](backend/feedforward.md), [alla inställningar](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/backend/.env.example) och [drift och deploy](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/DRIFT.md).

## Frontend

Frontend är byggd med React, TypeScript och Vite.

### Förutsättningar

För att köra projektet lokalt behöver du ha följande installerat:

- Node.js och npm
- Docker, exempelvis Docker Desktop

### Starta backend och databas

Se till att Docker är igång. Öppna en terminal i projektets rotmapp och kör:

```bash
cd infra
docker compose up --build -d
```

Backend nås på `http://localhost:5077`. Låt Docker-containrarna vara igång medan du använder applikationen.

### Installera och starta frontend

1. Öppna en ny terminal i projektets rotmapp och gå till frontend:

   ```bash
   cd frontend
   ```

2. Installera projektets beroenden:

   ```bash
   npm install
   ```

3. Kontrollera att `VITE_API_URL` i `frontend/.env.development` pekar på backend:

   ```env
   VITE_API_URL=http://localhost:5077
   ```

4. Starta utvecklingsservern:

   ```bash
   npm run dev
   ```

5. Öppna adressen som visas i terminalen, vanligtvis [http://localhost:5173](http://localhost:5173).

Stoppa utvecklingsservern med `Ctrl + C`.

### Inloggning

#### Personnummer och PIN-kod

Ange personnummer i formatet `ÅÅÅÅMMDD-XXXX` och tillhörande PIN-kod. Klicka sedan på **Logga in**. Vid lyckad inloggning kommer du till dashboarden.

Följande testkonton finns när backendens demodata har lagts in i databasen:

> Kontona nedan är fiktiva testkonton för lokal utveckling.

| Användare | Personnummer | PIN-kod |
|-----------|--------------|---------|
| Demo Kund | 19850615-5678 | 1234 |
| Anna Lindqvist | 19880312-4421 | 1234 |
| Erik Johansson | 19810927-1136 | 1234 |
| Sara Nilsson | 19930504-7782 | 1234 |
| Omar Hassan | 19790218-3355 | 1234 |

#### BankID i utvecklingsläge

Den lokala Docker-konfigurationen använder som standard BankID i mock-läge. Då kan du simulera en inloggning utan BankID-appen:

1. Klicka på **Logga in med BankID**.
2. Ange testkontots personnummer i panelen **Mock-läge (utveckling)**.
3. Klicka på **Simulera godkännande**.

När inloggningen har godkänts kommer du till dashboarden.
