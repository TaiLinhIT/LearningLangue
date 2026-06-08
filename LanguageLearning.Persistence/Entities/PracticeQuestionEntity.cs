namespace LanguageLearning.Persistence.Entities;

public class PracticeQuestionEntity
{
    public int Id { get; set; }

    public int LessonId { get; set; }

    public string Prompt { get; set; } = string.Empty;

    public string OptionsJson { get; set; } = "[]";

    public int CorrectIndex { get; set; }

    public string Explanation { get; set; } = string.Empty;

    public LessonEntity? Lesson { get; set; }
}
