namespace LanguageLearning.Persistence.Entities;

public class PricingPlanEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Price { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string FeaturesJson { get; set; } = "[]";

    public bool Highlighted { get; set; }
}
