namespace LanguageLearning.Persistence.Entities;

public class VocabularyItemEntity
{
    public int Id { get; set; }

    public string Term { get; set; } = string.Empty;

    public string Meaning { get; set; } = string.Empty;

    public string Phonetic { get; set; } = string.Empty;

    public string Example { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public bool Learned { get; set; }

    public LearningLanguageEntity? Language { get; set; }
}
