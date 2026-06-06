namespace LanguageLearning.Persistence.Entities;

public class LearningLanguageEntity
{
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string NativeName { get; set; } = string.Empty;

    public string Flag { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Learners { get; set; }

    public string Difficulty { get; set; } = string.Empty;
}
