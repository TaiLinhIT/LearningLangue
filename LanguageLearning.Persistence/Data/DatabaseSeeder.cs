using System.Text.Json;
using LanguageLearning.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Persistence.Data;

public class DatabaseSeeder(LearningDbContext dbContext, DemoLearningCatalogService demoCatalog)
{
    public async Task<SeedResult> EnsureCreatedAndSeedAsync(CancellationToken cancellationToken = default)
    {
        var created = await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        await CreateTablesIfMissingAsync(cancellationToken);
        var inserted = 0;

        if (!await dbContext.Languages.AnyAsync(cancellationToken))
        {
            dbContext.Languages.AddRange(demoCatalog.GetLanguages().Select(language => new LearningLanguageEntity
            {
                Code = language.Code,
                Name = language.Name,
                NativeName = language.NativeName,
                Flag = language.Flag,
                Description = language.Description,
                Learners = language.Learners,
                Difficulty = language.Difficulty
            }));
            inserted += demoCatalog.GetLanguages().Count;
        }

        if (!await dbContext.Lessons.AnyAsync(cancellationToken))
        {
            foreach (var lesson in demoCatalog.GetLessons())
            {
                dbContext.Lessons.Add(new LessonEntity
                {
                    Id = lesson.Id,
                    Title = lesson.Title,
                    LanguageCode = lesson.LanguageCode,
                    Level = lesson.Level,
                    Topic = lesson.Topic,
                    Skill = lesson.Skill,
                    DurationMinutes = lesson.DurationMinutes,
                    IsPremium = lesson.IsPremium,
                    Summary = lesson.Summary,
                    ExamplesJson = JsonSerializer.Serialize(lesson.Examples),
                    Questions = lesson.Questions.Select(question => new PracticeQuestionEntity
                    {
                        Prompt = question.Prompt,
                        OptionsJson = JsonSerializer.Serialize(question.Options),
                        CorrectIndex = question.CorrectIndex,
                        Explanation = question.Explanation
                    }).ToList()
                });
                inserted++;
                inserted += lesson.Questions.Count;
            }
        }

        if (!await dbContext.VocabularyItems.AnyAsync(cancellationToken))
        {
            dbContext.VocabularyItems.AddRange(demoCatalog.GetVocabulary().Select(item => new VocabularyItemEntity
            {
                Term = item.Term,
                Meaning = item.Meaning,
                Phonetic = item.Phonetic,
                Example = item.Example,
                LanguageCode = item.LanguageCode,
                Topic = item.Topic,
                Learned = item.Learned
            }));
            inserted += demoCatalog.GetVocabulary().Count;
        }

        if (!await dbContext.PlacementQuestions.AnyAsync(cancellationToken))
        {
            dbContext.PlacementQuestions.AddRange(demoCatalog.GetPlacementQuestions().Select(question => new PlacementQuestionEntity
            {
                Prompt = question.Prompt,
                OptionsJson = JsonSerializer.Serialize(question.Options),
                CorrectIndex = question.CorrectIndex,
                Skill = question.Skill
            }));
            inserted += demoCatalog.GetPlacementQuestions().Count;
        }

        if (!await dbContext.LearningProgress.AnyAsync(cancellationToken))
        {
            var progress = demoCatalog.GetProgress();
            dbContext.LearningProgress.Add(new LearningProgressEntity
            {
                CurrentLevel = progress.CurrentLevel,
                LessonsCompleted = progress.LessonsCompleted,
                TotalLessons = progress.TotalLessons,
                AverageScore = progress.AverageScore,
                StreakDays = progress.StreakDays,
                WordsLearned = progress.WordsLearned,
                Skills = progress.Skills.Select(skill => new SkillProgressEntity
                {
                    Name = skill.Name,
                    Icon = skill.Icon,
                    Progress = skill.Progress,
                    Description = skill.Description
                }).ToList()
            });
            inserted++;
            inserted += progress.Skills.Count;
        }

        if (!await dbContext.PricingPlans.AnyAsync(cancellationToken))
        {
            dbContext.PricingPlans.AddRange(demoCatalog.GetPricingPlans().Select(plan => new PricingPlanEntity
            {
                Name = plan.Name,
                Price = plan.Price,
                Description = plan.Description,
                FeaturesJson = JsonSerializer.Serialize(plan.Features),
                Highlighted = plan.Highlighted
            }));
            inserted += demoCatalog.GetPricingPlans().Count;
        }

        if (!await dbContext.AdminMetrics.AnyAsync(cancellationToken))
        {
            dbContext.AdminMetrics.AddRange(demoCatalog.GetAdminMetrics().Select(metric => new AdminMetricEntity
            {
                Label = metric.Label,
                Value = metric.Value,
                Change = metric.Change
            }));
            inserted += demoCatalog.GetAdminMetrics().Count;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new SeedResult(created, inserted);
    }

    private async Task CreateTablesIfMissingAsync(CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            IF OBJECT_ID(N'dbo.LearningLanguages', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.LearningLanguages (
                    Code nvarchar(12) NOT NULL CONSTRAINT PK_LearningLanguages PRIMARY KEY,
                    Name nvarchar(120) NOT NULL,
                    NativeName nvarchar(120) NOT NULL,
                    Flag nvarchar(12) NOT NULL,
                    Description nvarchar(max) NOT NULL,
                    Learners int NOT NULL,
                    Difficulty nvarchar(60) NOT NULL
                );
            END;

            IF OBJECT_ID(N'dbo.Lessons', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Lessons (
                    Id int NOT NULL CONSTRAINT PK_Lessons PRIMARY KEY,
                    Title nvarchar(220) NOT NULL,
                    LanguageCode nvarchar(12) NOT NULL,
                    Level nvarchar(20) NOT NULL,
                    Topic nvarchar(120) NOT NULL,
                    Skill nvarchar(80) NOT NULL,
                    DurationMinutes int NOT NULL,
                    IsPremium bit NOT NULL,
                    Summary nvarchar(max) NOT NULL,
                    ExamplesJson nvarchar(max) NOT NULL,
                    CONSTRAINT FK_Lessons_LearningLanguages_LanguageCode
                        FOREIGN KEY (LanguageCode) REFERENCES dbo.LearningLanguages(Code)
                );
            END;

            IF OBJECT_ID(N'dbo.PracticeQuestions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PracticeQuestions (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PracticeQuestions PRIMARY KEY,
                    LessonId int NOT NULL,
                    Prompt nvarchar(500) NOT NULL,
                    OptionsJson nvarchar(max) NOT NULL,
                    CorrectIndex int NOT NULL,
                    Explanation nvarchar(max) NOT NULL,
                    CONSTRAINT FK_PracticeQuestions_Lessons_LessonId
                        FOREIGN KEY (LessonId) REFERENCES dbo.Lessons(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.VocabularyItems', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.VocabularyItems (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_VocabularyItems PRIMARY KEY,
                    Term nvarchar(160) NOT NULL,
                    Meaning nvarchar(220) NOT NULL,
                    Phonetic nvarchar(160) NOT NULL,
                    Example nvarchar(max) NOT NULL,
                    LanguageCode nvarchar(12) NOT NULL,
                    Topic nvarchar(120) NOT NULL,
                    Learned bit NOT NULL,
                    CONSTRAINT FK_VocabularyItems_LearningLanguages_LanguageCode
                        FOREIGN KEY (LanguageCode) REFERENCES dbo.LearningLanguages(Code)
                );
            END;

            IF OBJECT_ID(N'dbo.PlacementQuestions', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PlacementQuestions (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PlacementQuestions PRIMARY KEY,
                    Prompt nvarchar(max) NOT NULL,
                    OptionsJson nvarchar(max) NOT NULL,
                    CorrectIndex int NOT NULL,
                    Skill nvarchar(80) NOT NULL
                );
            END;

            IF OBJECT_ID(N'dbo.LearningProgress', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.LearningProgress (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_LearningProgress PRIMARY KEY,
                    CurrentLevel nvarchar(20) NOT NULL,
                    LessonsCompleted int NOT NULL,
                    TotalLessons int NOT NULL,
                    AverageScore int NOT NULL,
                    StreakDays int NOT NULL,
                    WordsLearned int NOT NULL
                );
            END;

            IF OBJECT_ID(N'dbo.SkillProgress', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.SkillProgress (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_SkillProgress PRIMARY KEY,
                    LearningProgressId int NOT NULL,
                    Name nvarchar(120) NOT NULL,
                    Icon nvarchar(40) NOT NULL,
                    Progress int NOT NULL,
                    Description nvarchar(max) NOT NULL,
                    CONSTRAINT FK_SkillProgress_LearningProgress_LearningProgressId
                        FOREIGN KEY (LearningProgressId) REFERENCES dbo.LearningProgress(Id) ON DELETE CASCADE
                );
            END;

            IF OBJECT_ID(N'dbo.PricingPlans', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.PricingPlans (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_PricingPlans PRIMARY KEY,
                    Name nvarchar(120) NOT NULL,
                    Price nvarchar(80) NOT NULL,
                    Description nvarchar(max) NOT NULL,
                    FeaturesJson nvarchar(max) NOT NULL,
                    Highlighted bit NOT NULL
                );
            END;

            IF OBJECT_ID(N'dbo.AdminMetrics', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.AdminMetrics (
                    Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_AdminMetrics PRIMARY KEY,
                    Label nvarchar(120) NOT NULL,
                    Value nvarchar(80) NOT NULL,
                    Change nvarchar(120) NOT NULL
                );
            END;
            """,
            cancellationToken);
    }
}

public record SeedResult(bool DatabaseCreated, int RowsInserted);
