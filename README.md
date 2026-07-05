# LovePlaceApp (MAUI Client + API)

Repository con:

- client .NET MAUI (`LovePlaceApp.csproj`)
- backend ASP.NET Core Web API (`LovePlaces.Api/LovePlaces.Api.csproj`)

## Configurazione ambienti

L'app usa due file di configurazione:

- `appsettings.json`: configurazione base (default/production)
- `appsettings.Development.json`: override usato in Debug

Chiave usata:

- `Api:BaseUrl`

In Debug, il client punta a:

- `https://localhost:7001/`

## Avvio in Debug (Windows)

```powershell
dotnet run --project LovePlaceApp.csproj -f net10.0-windows10.0.19041.0 -c Debug
```

## Nota su database ("ribaltare il DB")

Il backend API include EF Core SQL Server con migration iniziale (`InitialCreate`).

Comandi tipici (dalla root del repository):

```powershell
dotnet ef migrations add <NomeMigration> --project LovePlaces.Api --startup-project LovePlaces.Api
dotnet ef database update --project LovePlaces.Api --startup-project LovePlaces.Api
```

Connection string di default in `LovePlaces.Api/appsettings.json`:

- `Server=(localdb)\\MSSQLLocalDB;Database=LovePlacesDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`

Se vuoi, nel prossimo step posso collegare il client al backend effettivo (URL dev/stage/prod) e rimuovere il fallback locale usato in debug.

## SonarCloud (prima analisi)

Workflow pronto in `.github/workflows/sonarcloud.yml`.

Prerequisiti repository GitHub:

1. Crea il secret `SONAR_TOKEN` in GitHub:
	- Repository -> Settings -> Secrets and variables -> Actions -> New repository secret
2. Verifica che il progetto SonarCloud esista con key `MatteoHiro_LovePlaces` nell'organizzazione `matteohiro`.
3. Esegui push su `develop` oppure apri una PR verso `develop/main`.

Il workflow lancerà la scansione e popolerà il progetto su SonarCloud (issue, metriche, quality gate).

Nota: per questo progetto usare analisi CI (workflow GitHub Actions). Se SonarCloud mostra avvisi su "Automatic analysis", puoi ignorarli o disabilitare l'opzione nel progetto SonarCloud.
