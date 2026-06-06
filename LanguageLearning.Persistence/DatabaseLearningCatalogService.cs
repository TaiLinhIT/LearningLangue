using System.Data.Common;
using System.Text.Json;
using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;
using LanguageLearning.Persistence.Data;
using LanguageLearning.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LanguageLearning.Persistence;

public class DatabaseLearningCatalogService(
    LearningDbContext dbContext,
    DemoLearningCatalogService fallback,
    ILogger<DatabaseLearningCatalogService> logger) : ILearningCatalogService
{
    public IReadOnlyList<LanguageOption> GetLanguages() =>
        ReadList(
            () => dbContext.Languages
                .AsNoTracking()
                .OrderBy(language => language.Name)
                .Select(language => new LanguageOption(
                    language.Code,
                    language.Name,
                    language.NativeName,
                    language.Flag,
                    language.Description,
                    language.Learners,
                    language.Difficulty))
                .ToList(),
            fallback.GetLanguages);

    public IReadOnlyList<Lesson> GetLessons(string? languageCode = null) =>
        ReadList(
            () =>
            {
                var query = dbContext.Lessons
                    .AsNoTracking()
                    .Include(lesson => lesson.Questions)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(languageCode))
                {
                    query = query.Where(lesson => lesson.LanguageCode == languageCode);
                }

                return query
                    .OrderBy(lesson => lesson.Level)
                    .ThenBy(lesson => lesson.Id)
                    .ToList()
                    .Select(MapLesson)
                    .ToList();
            },
            () => fallback.GetLessons(languageCode));

    public Lesson? GetLesson(int id) =>
        ReadSingle(
            () => dbContext.Lessons
                .AsNoTracking()
                .Include(lesson => lesson.Questions)
                .Where(lesson => lesson.Id == id)
                .AsEnumerable()
                .Select(MapLesson)
                .FirstOrDefault(),
            () => fallback.GetLesson(id));

    public IReadOnlyList<VocabularyItem> GetVocabulary(string? query = null) =>
        ReadList(
            () =>
            {
                var vocabulary = dbContext.VocabularyItems.AsNoTracking();

                if (!string.IsNullOrWhiteSpace(query))
                {
                    vocabulary = vocabulary.Where(item =>
                        item.Term.Contains(query)
                        || item.Meaning.Contains(query)
                        || item.Topic.Contains(query));
                }

                return vocabulary
                    .OrderBy(item => item.Term)
                    .Select(item => new VocabularyItem(
                        item.Term,
                        item.Meaning,
                        item.Phonetic,
                        item.Example,
                        item.LanguageCode,
                        item.Topic,
                        item.Learned))
                    .ToList();
            },
            () => fallback.GetVocabulary(query));

    public IReadOnlyList<PlacementQuestion> GetPlacementQuestions() =>
        ReadList(
            () => dbContext.PlacementQuestions
                .AsNoTracking()
                .OrderBy(question => question.Id)
                .AsEnumerable()
                .Select(question => new PlacementQuestion(
                    question.Prompt,
                    ReadJsonList(question.OptionsJson),
                    question.CorrectIndex,
                    question.Skill))
                .ToList(),
            fallback.GetPlacementQuestions);

    public LearningProgress GetProgress() =>
        ReadSingle(
            () => dbContext.LearningProgress
                .AsNoTracking()
                .Include(progress => progress.Skills)
                .OrderBy(progress => progress.Id)
                .AsEnumerable()
                .Select(progress => new LearningProgress(
                    progress.CurrentLevel,
                    progress.LessonsCompleted,
                    progress.TotalLessons,
                    progress.AverageScore,
                    progress.StreakDays,
                    progress.WordsLearned,
                    progress.Skills
                        .OrderBy(skill => skill.Id)
                        .Select(skill => new SkillTrack(skill.Name, skill.Icon, skill.Progress, skill.Description))
                        .ToList()))
                .FirstOrDefault(),
            fallback.GetProgress) ?? fallback.GetProgress();

    public IReadOnlyList<PricingPlan> GetPricingPlans() =>
        ReadList(
            () => dbContext.PricingPlans
                .AsNoTracking()
                .OrderBy(plan => plan.Id)
                .AsEnumerable()
                .Select(plan => new PricingPlan(
                    plan.Name,
                    plan.Price,
                    plan.Description,
                    ReadJsonList(plan.FeaturesJson),
                    plan.Highlighted))
                .ToList(),
            fallback.GetPricingPlans);

    public IReadOnlyList<AdminMetric> GetAdminMetrics() =>
        ReadList(
            () => dbContext.AdminMetrics
                .AsNoTracking()
                .OrderBy(metric => metric.Id)
                .Select(metric => new AdminMetric(metric.Label, metric.Value, metric.Change))
                .ToList(),
            fallback.GetAdminMetrics);

    private static Lesson MapLesson(LessonEntity lesson) =>
        new(
            lesson.Id,
            lesson.Title,
            lesson.LanguageCode,
            lesson.Level,
            lesson.Topic,
            lesson.Skill,
            lesson.DurationMinutes,
            lesson.IsPremium,
            lesson.Summary,
            ReadJsonList(lesson.ExamplesJson),
            lesson.Questions
                .OrderBy(question => question.Id)
                .Select(question => new PracticeQuestion(
                    question.Prompt,
                    ReadJsonList(question.OptionsJson),
                    question.CorrectIndex,
                    question.Explanation))
                .ToList());

    private IReadOnlyList<T> ReadList<T>(Func<IReadOnlyList<T>> databaseQuery, Func<IReadOnlyList<T>> fallbackQuery)
    {
        try
        {
            var databaseItems = databaseQuery();
            return databaseItems.Count > 0 ? databaseItems : fallbackQuery();
        }
        catch (Exception ex) when (IsDatabaseProblem(ex))
        {
            logger.LogWarning(ex, "Database catalog read failed. Returning demo catalog data.");
            return fallbackQuery();
        }
    }

    private T? ReadSingle<T>(Func<T?> databaseQuery, Func<T?> fallbackQuery)
    {
        try
        {
            return databaseQuery() ?? fallbackQuery();
        }
        catch (Exception ex) when (IsDatabaseProblem(ex))
        {
            logger.LogWarning(ex, "Database catalog read failed. Returning demo catalog data.");
            return fallbackQuery();
        }
    }

    private static bool IsDatabaseProblem(Exception ex) =>
        ex is DbException or InvalidOperationException or TimeoutException;

    private static IReadOnlyList<string> ReadJsonList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
