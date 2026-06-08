# LanguageLearning

Clean Architecture Blazor website for learning foreign languages.

The solution is split into Domain, Application, Persistence, Infrastructure, API and WebUI projects so features can grow without putting everything into one Blazor project.

Start here:

```powershell
dotnet build LanguageLearning.slnx
dotnet run --project LanguageLearning.WebUI\LanguageLearning.WebUI.csproj --launch-profile http
```

Website URL: `http://localhost:5018`.
API Swagger URL: `http://localhost:5088/swagger`.
# LearningLangue
