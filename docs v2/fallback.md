# Fallback

Teamets fallback är en inspelad demo som vi gjorde tidigare och lämnar in med leveransen. Om livedemon krånglar:

1. Prova stage: https://nordiska-team3-dev.team.chas-challenge.comerit.se (`/api/health` ska svara).
2. Kör lokalt: `cd infra`, `docker compose up --build`, öppna `http://localhost:5077` och logga in med en demokund ([demo-data.md](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/docs/demo-data.md)).


| Problem | Orsak och åtgärd |
|---------|------------------|
| Docker hittas inte | Starta Docker Desktop |
| Porten är upptagen | Stäng programmet som använder 5077 eller 5432 |
| Ingen ränta syns | Servern når inte api.riksbank.se. Resten fungerar |
| Kontot är låst | Fem fel PIN låser det i fem minuter. Vänta eller använd en annan demokund |
| BankID ger fel | BankID är avstängt på stage. Logga in med PIN |
| Skatterapporten blir `FAILED` | Signeringsnyckeln saknas, se [local-development.md](https://github.com/chas-challenge-2026/nordiska-team3/blob/dev/docs/local-development.md), avsnitt 3. Lokalt hjälper `docker compose down -v` |
| Gamla ändringar syns | Kör `docker compose up --build` |
