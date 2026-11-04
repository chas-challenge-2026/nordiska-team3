# Beslutslogg

## Så tog vi beslut

Mötesanteckningar fördes fram till den 22 september. Sedan minskade deltagandet och teamledarna dokumenterade inte längre, så de som var kvar höll i stället dagliga möten om vad som skulle prioriteras. Besluten sedan dess finns i pull requests. Enligt vår Definition of Done är en issue klar först när testning, code review och merge är gjorda.

| Datum | Beslut | Varför | PR |
|-------|--------|--------|----|
| 2026-09-08 | Saldot räknas ur en huvudbok (`LedgerEntry`), med radlås och en transaktion per order | v1 kunde få fel saldo vid samtidiga insättningar | #86, #89 |
| 2026-09-08 | PIN sparas med BCrypt | v1 använde MD5 | #59 |
| 2026-09-09 | Allt körs i Docker så att alla har samma miljö | Alla ska "få samma" | Möte, #152, #333 |
| 2026-09-17 | Skatterapporten skapas i en kö med en bakgrundstjänst | v1 blockerade request-tråden med `Thread.Sleep` | #198, #334 |
| 2026-09-21 | E-post läggs i en kö och skickas av en bakgrundstjänst. Utskicket simuleras | v1 skickade mail direkt utan kö och retry | #209, #338 |
| 2026-09-30 | Överföring mellan konton är en atomär operation | Pengarna ska gå ur ett konto och in på det andra, eller inte alls | #251, #335 |
| 2026-10-01 | Hemligheter skapas i Docker-volymer och ligger aldrig i repot | Ingen ska behöva dela lösenord eller nycklar | #255, #323, #345 |
| 2026-10-06 | Rate limiting och säkerhetsheaders | Skydd mot gissning och missbruk | #287, #288 |
| 2026-10-06 | Regelbaserad FAQ-sökning med poäng och tröskel. Under tröskeln hänvisas kunden till kundtjänst | v1 tog första nyckelordsträffen | #282 |
| 2026-10-07 | Personnummer krypteras (AES-GCM) och maskas i svaren | Känsliga uppgifter | #319 |
| 2026-10-08 | Audit-logg med hashkedja och en trigger som bara tillåter nya rader | Ändringar i efterhand ska synas | #356 |
| 2026-10-10 | Ränta från Riksbankens styrränta plus påslag per kontotyp, utbetald 1 januari | Rörlig ränta som följer marknaden | #357, #363 |
| 2026-10-13 | Rapporten signeras och verifieras innan den blir `READY` | Rapporten ska gå att bevisa äkta | #362, #367 |
| 2026-10-13 | PIN-låsning efter fem fel i fem minuter | En PIN går att gissa utan spärr. Baksidan: någon kan låsa ut en kund en stund | #368 |
| 2026-10-13 | Refresh-token hashad per enhet med rotation, 30 minuter | En stulen token ska ha kort livslängd | #369, #370, #389 |
| 2026-10-14 | API:et vägrar starta om en nyckel saknas när databasen har data | En ny nyckel gör personnummer oläsbara och gamla signaturer ogiltiga. Därför måste nycklarna säkerhetskopieras | #319, #356, #372 |
| 2026-10-14 | Valfri `Idempotency-Key` på insättning, uttag och överföring | En omsändning ska inte bokföras två gånger | #383 |
| 2026-10-14 | Ett test låser vilka endpoints som är öppna | Inget ska bli öppet av misstag | #377 |
| 2026-10-28 | BankID i lägena `Off`, `Mock` och `Test` | Riktig BankID kräver avtal och certifikat | #391 |
