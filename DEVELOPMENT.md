# Development

## Structure

- `LanguageLearning.Domain`: entity and record models for lessons, vocabulary, placement tests, progress and pricing.
- `LanguageLearning.Application`: application contracts such as `ILearningCatalogService`.
- `LanguageLearning.Persistence`: demo data implementation. Replace this with EF Core repositories when adding a database.
- `LanguageLearning.Infrastructure`: integrations such as email, storage, payment and speech services.
- `LanguageLearning.API`: minimal API endpoints for mobile apps or external clients.
- `LanguageLearning.WebUI`: Blazor Web App user interface.

## Run WebUI

```powershell
dotnet run --project LanguageLearning.WebUI\LanguageLearning.WebUI.csproj --launch-profile http
```

Open `http://localhost:5018`.

## Run API

```powershell
dotnet run --project LanguageLearning.API\LanguageLearning.API.csproj
```

Use endpoints such as `/api/languages`, `/api/lessons`, `/api/vocabulary`, `/api/progress`.
