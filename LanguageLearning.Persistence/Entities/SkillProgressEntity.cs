namespace LanguageLearning.Persistence.Entities;

public class SkillProgressEntity
{
    public int Id { get; set; }

    public int LearningProgressId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Icon { get; set; } = string.Empty;

    public int Progress { get; set; }

    public string Description { get; set; } = string.Empty;

    public LearningProgressEntity? LearningProgress { get; set; }
}
