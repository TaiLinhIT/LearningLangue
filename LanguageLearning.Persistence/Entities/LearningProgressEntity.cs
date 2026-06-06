namespace LanguageLearning.Persistence.Entities;

public class LearningProgressEntity
{
    public int Id { get; set; }

    public string CurrentLevel { get; set; } = string.Empty;

    public int LessonsCompleted { get; set; }

    public int TotalLessons { get; set; }

    public int AverageScore { get; set; }

    public int StreakDays { get; set; }

    public int WordsLearned { get; set; }

    public List<SkillProgressEntity> Skills { get; set; } = [];
}
