namespace LanguageLearning.Domain;

public record LanguageOption(
    string Code,
    string Name,
    string NativeName,
    string Flag,
    string Description,
    int Learners,
    string Difficulty);

public record SkillTrack(string Name, string Icon, int Progress, string Description);

public record Lesson(
    int Id,
    string Title,
    string LanguageCode,
    string Level,
    string Topic,
    string Skill,
    int DurationMinutes,
    bool IsPremium,
    string Summary,
    IReadOnlyList<string> Examples,
    IReadOnlyList<PracticeQuestion> Questions);

public record PracticeQuestion(
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectIndex,
    string Explanation);

public record VocabularyItem(
    string Term,
    string Meaning,
    string Phonetic,
    string Example,
    string LanguageCode,
    string Topic,
    bool Learned);

public record PlacementQuestion(
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectIndex,
    string Skill);

public record LearningProgress(
    string CurrentLevel,
    int LessonsCompleted,
    int TotalLessons,
    int AverageScore,
    int StreakDays,
    int WordsLearned,
    IReadOnlyList<SkillTrack> Skills);

public record PricingPlan(
    string Name,
    string Price,
    string Description,
    IReadOnlyList<string> Features,
    bool Highlighted);

public record AdminMetric(string Label, string Value, string Change);
