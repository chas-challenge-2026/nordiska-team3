# Köra lokalt

Backend kan köras på två sätt. Docker är det enkla sättet och det som skatterapporterna kräver. Utan Docker kan du starta API:et från Visual Studio eller med `dotnet run`, men då måste du själv ange databas och nycklar.

## 1. Allt i Docker (rekommenderat)

```
cd infra
docker compose up --build
```

API:et och frontend svarar på http://localhost:5077. Databaslösenordet och nycklarna skapas automatiskt första gången och sparas i Docker-volymer, så du behöver inte ange något själv. Lokalt körs appen som Development och lägger in en testanvändare: personnummer `19900101-1234` och PIN `1234`.

Stoppa med `docker compose down`. `docker compose down -v` tar också bort databasen och nycklarna, och de ska alltid försvinna tillsammans (se avsnitt 3).

## 2. API:et utanför Docker

Använd en egen databas och inte den som `docker compose` startar. Nyckeln som krypterar personnummer måste höra ihop med databasen, och blandar du dem startar inte API:et.

1. Starta en databas. Porten 5433 gör att den inte krockar med compose-databasen.

```
docker run --name nordiska-localdb -e POSTGRES_USER=nordiska -e POSTGRES_PASSWORD=valj-ett-losenord -e POSTGRES_DB=nordiska -p 5433:5432 -d postgres:15
```

2. Skapa en slumpad nyckel i PowerShell. Kör kommandot två gånger, en nyckel till varje inställning nedan.

```
$b = New-Object byte[] 32; (New-Object System.Security.Cryptography.RNGCryptoServiceProvider).GetBytes($b); [Convert]::ToBase64String($b)
```

3. Lägg in värdena med user-secrets. De sparas på din dator utanför repot. Stå i mappen `backend/NordiskaPortal.API`.

```
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5433;Database=nordiska;Username=nordiska;Password=valj-ett-losenord"
dotnet user-secrets set "Jwt:Key" "<första nyckeln>"
dotnet user-secrets set "PersonalNumberProtection:Key" "<andra nyckeln>"
```

4. Starta API:et från Visual Studio eller med `dotnet run`. Det svarar på http://localhost:5077, och Swagger finns på `/swagger`. Kontrollera att http://localhost:5077/health svarar `Healthy`.

Sätt alltid nycklarna lokalt. Annars skapar API:et nyckelfiler under `C:\secrets` (roten på disken).

Skatterapporter fungerar bara i Docker, eftersom den inbyggda PDF-generatorn byggs i Docker-bilden.

## 3. Hemligheter i Docker, stage och prod

Databaslösenordet och nycklarna genereras första gången appen startar och sparas i två volymer: `db_secret` (databaslösenordet) och `api_secrets` (`jwt.key` och `pn.key`). Ta inte bort en av dem ensam.

- Försvinner `api_secrets` medan databasen finns kvar går personnummer inte att läsa utan nyckeln, så API:et vägrar starta.
- En ny `jwt.key` loggar bara ut alla inloggade.

## 4. Alla inställningar

Namn och förklaringar finns i `backend/.env.example`.

## 5. Tester mot riktig Postgres

Samtidighetstesterna för överföring körs bara om miljövariabeln `NORDISKA_TEST_DB` pekar på en databas med "test" i namnet. Utan den hoppas de över.

```
docker run --name nordiska-testdb -e POSTGRES_PASSWORD=test -e POSTGRES_DB=nordiska_test -p 55432:5432 -d postgres:15
```

```
$env:NORDISKA_TEST_DB = "Host=localhost;Port=55432;Database=nordiska_test;Username=postgres;Password=test"
```

Kör sedan `dotnet test` i mappen `backend`. Ta bort databasen efteråt med `docker rm -f nordiska-testdb`. I CI körs de automatiskt.