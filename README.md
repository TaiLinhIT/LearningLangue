# LanguageLearning

Clean Architecture LMS for a language center, built with Blazor Web App, ASP.NET Core API, EF Core, SQL Server, JWT/cookie authentication, one-device session validation, and a mock AI scoring service.

## Projects

- `LanguageLearning.Domain`: entities, roles, constants, read models.
- `LanguageLearning.Application`: service contracts.
- `LanguageLearning.Infrastructure`: EF Core DbContext, seed data, auth, catalog, AI scoring, experience services.
- `LanguageLearning.API`: JWT API endpoints and Swagger.
- `LanguageLearning.WebUI`: Blazor Server UI.
- `LanguageLearning.Persistence`: legacy persistence shell kept for compatibility.

## Build

The repo targets .NET 9. On this machine the verified SDK is `9.0.311`.

```powershell
dotnet build LanguageLearning.slnx -m:1 -p:BuildInParallel=false
```

The `-m:1` flag avoids local MSBuild graph/file-lock issues seen when solution builds run projects in parallel.

## Run

Web UI:

```powershell
dotnet run --project LanguageLearning.WebUI\LanguageLearning.WebUI.csproj --launch-profile http
```

Open `http://localhost:5018`.

API:

```powershell
dotnet run --project LanguageLearning.API\LanguageLearning.API.csproj --launch-profile http
```

Open `http://localhost:5088/swagger`.

## Demo Accounts

- Admin: `admin@linguaflow.local` / `Admin@123`
- Teacher: `teacher@linguaflow.local` / `Teacher@123`
- Receptionist: `reception@linguaflow.local` / `Reception@123`
- Student: `learner@linguaflow.local` / `Learner@123`
- Other students: `minh@linguaflow.local`, `hana@linguaflow.local`, `an@linguaflow.local`, `bao@linguaflow.local` / `Student@123`

## Database

Default connection string uses SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=LanguageLearning;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;Encrypt=False
```

The app runs `Database.Migrate()` on startup and then ensures demo users/progress seed data.

Manual migration command:

```powershell
dotnet tool restore
dotnet ef database update --project LanguageLearning.Infrastructure --startup-project LanguageLearning.WebUI --context LanguageLearningDbContext
```

If `dotnet ef` complains about a missing or old tool, run:

```powershell
dotnet tool update dotnet-ef --version 9.0.16
```

## Current Feature Surface

- Public home, catalog, course detail, pricing, login/register/forgot password.
- Cookie auth in WebUI and JWT auth in API.
- One active session per account via `CurrentSessionToken`, `UserSessions`, and request validation.
- Student dashboard, course roadmap, lesson player, quiz submit, AI sentence scoring.
- Student tools: vocabulary library, flashcards, IPA pronunciation, class overview/discussion, class/center ranking, rewards, review mistakes, profile.
- Admin pages for users, courses, lessons, vocabulary, questions, reports, and login session history.
