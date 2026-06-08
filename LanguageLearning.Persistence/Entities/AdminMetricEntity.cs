namespace LanguageLearning.Persistence.Entities;

public class AdminMetricEntity
{
    public int Id { get; set; }

    public string Label { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string Change { get; set; } = string.Empty;
}
