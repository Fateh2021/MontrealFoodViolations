# MontrealFoodViolations

MontrealFoodViolations is a .NET 10 ASP.NET Core Web API that automatically downloads, parses, and synchronizes the public Montreal food safety violations dataset into SQLite.

## Purpose

The application retrieves the latest CSV published by the City of Montreal, detects whether it changed, updates the SQLite database without duplicating records, and exposes the data through REST endpoints. The scheduler runs in the background and can also be triggered manually via API.

## Official source

The dataset comes directly from the City of Montreal:

https://data.montreal.ca/dataset/05a9e718-6810-4e73-8bb9-5955efeb91a0/resource/7f939a08-be8a-45e1-b208-d8744dca8fc6/download/violations.csv

The CSV is not committed to Git. The app downloads the live file at runtime.

## Real dataset analysis

The actual file header includes these columns:

- id_poursuite
- business_id
- date
- description
- adresse
- date_jugement
- etablissement
- montant
- proprietaire
- ville
- statut
- date_statut
- categorie

The unique record identity is the real field `id_poursuite`, which is the dataset’s stable identifier. This is the key used for inserts, updates, and duplicate prevention.

## Architecture

- ASP.NET Core Web API
- EF Core + SQLite
- BackgroundService for scheduled sync
- HttpClient via IHttpClientFactory
- Swagger/OpenAPI
- xUnit tests
- Dependency injection and logging

## Background synchronization

The scheduler is implemented as `ViolationSyncBackgroundService` and runs based on the `ViolationSync:IntervalHours` value in configuration. The default is 24 hours.

The loop follows this flow:

1. wait for the configured interval
2. download the latest CSV
3. validate the response
4. parse the CSV
5. compare against SQLite
6. insert new rows
7. update changed rows
8. log the result
9. wait for the next interval

If the download fails, the app logs the issue and keeps the existing data intact.

## Database

SQLite is configured through the connection string in `appsettings.json`.

The main entities are:

- `Violation`
- `DatasetSyncState`

A migration is required to create the schema.

## API endpoints

- `GET /api/violations?page=1&pageSize=25`
- `GET /api/violations/{id}`
- `GET /api/violations/business/{businessId}`
- `GET /api/violations/search`
- `GET /api/violations/export`
- `GET /api/violations/stats`
- `POST /api/sync`
- `GET /api/sync/status`

## Installation

1. Install the .NET SDK (the project targets .NET 10 in this environment).
2. Restore packages.
3. Apply migrations.
4. Run the API.

## Migrations

From the project root:

```bash
dotnet ef database update --project src/MontrealFoodViolations.Api/MontrealFoodViolations.Api.csproj
```

## Run

```bash
dotnet run --project src/MontrealFoodViolations.Api/MontrealFoodViolations.Api.csproj
```

## Tests

```bash
dotnet test
```

## Configuration

```json
{
  "ViolationSync": {
    "Enabled": true,
    "IntervalHours": 24
  },
  "MontrealDataset": {
    "ViolationsUrl": "https://data.montreal.ca/.../violations.csv"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=montrealfoodviolations.db"
  }
}
```

## Synchronization strategy

The sync process is idempotent. If the same file is downloaded again, existing rows are detected as unchanged and no duplicates are created. New rows are inserted, changed rows are updated, and no data is deleted unless explicitly required by a future design change.

## Notes

This project is structured to allow later evolution toward PostgreSQL, Azure, or another production environment without rewriting the whole application.

## Development

This project was designed and architected by the author. AI coding tools (Cursor) were used to accelerate boilerplate generation, documentation, UI iterations, and refactoring suggestions.

Key decisions made manually:

- clean architecture (Domain / Application / Infrastructure / API)
- idempotent sync strategy using `id_poursuite`
- data modeling from the official Montreal open dataset
- API endpoint design and search experience

See [DOCUMENTATION.md](DOCUMENTATION.md) for the full API guide in French.
