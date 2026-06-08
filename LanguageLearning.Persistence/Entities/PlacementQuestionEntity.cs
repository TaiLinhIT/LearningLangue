namespace LanguageLearning.Persistence.Entities;

public class PlacementQuestionEntity
{
    public int Id { get; set; }

    public string Prompt { get; set; } = string.Empty;

    public string OptionsJson { get; set; } = "[]";

    public int CorrectIndex { get; set; }

    public string Skill { get; set; } = string.Empty;
}
