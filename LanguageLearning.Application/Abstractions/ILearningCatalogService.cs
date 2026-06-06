using LanguageLearning.Domain;

namespace LanguageLearning.Application.Abstractions;

public interface ILearningCatalogService
{
    IReadOnlyList<LanguageOption> GetLanguages();

    IReadOnlyList<Lesson> GetLessons(string? languageCode = null);

    Lesson? GetLesson(int id);

    IReadOnlyList<VocabularyItem> GetVocabulary(string? query = null);

    IReadOnlyList<PlacementQuestion> GetPlacementQuestions();

    LearningProgress GetProgress();

    IReadOnlyList<PricingPlan> GetPricingPlans();

    IReadOnlyList<AdminMetric> GetAdminMetrics();
}
