namespace LanguageLearning.Persistence.Entities;

public class LessonEntity
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public string Level { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public string Skill { get; set; } = string.Empty;

    public int DurationMinutes { get; set; }

    public bool IsPremium { get; set; }

    public string Summary { get; set; } = string.Empty;

    public string ExamplesJson { get; set; } = "[]";

    public LearningLanguageEntity? Language { get; set; }

    public List<PracticeQuestionEntity> Questions { get; set; } = [];
}
