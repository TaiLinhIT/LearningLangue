namespace LanguageLearning.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Learner = "Learner";
}

public static class LessonTypes
{
    public const string Vocabulary = "Vocabulary";
    public const string Listening = "Listening";
    public const string Speaking = "Speaking";
    public const string Grammar = "Grammar";
}

public static class ProgressStatuses
{
    public const string NotStarted = "NotStarted";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
}

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = Roles.Learner;
    public string LearningGoal { get; set; } = "Giao tiep hang ngay";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<UserCourse> UserCourses { get; set; } = [];
    public ICollection<UserLessonProgress> LessonProgress { get; set; } = [];
}

public class Language
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string FlagIcon { get; set; } = string.Empty;
    public ICollection<Course> Courses { get; set; } = [];
}

public class Course
{
    public int Id { get; set; }
    public int LanguageId { get; set; }
    public Language? Language { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Level { get; set; } = "A1";
    public string? ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; }
    public ICollection<Unit> Units { get; set; } = [];
}

public class Unit
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public string Title { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public ICollection<Lesson> Lessons { get; set; } = [];
}

public class Lesson
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public Unit? Unit { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string LessonType { get; set; } = LessonTypes.Vocabulary;
    public int XPReward { get; set; }
    public int SortOrder { get; set; }
    public bool IsLocked { get; set; }
    public ICollection<Vocabulary> Vocabulary { get; set; } = [];
    public ICollection<Question> Questions { get; set; } = [];
}

public class Vocabulary
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public string Word { get; set; } = string.Empty;
    public string Meaning { get; set; } = string.Empty;
    public string? Pronunciation { get; set; }
    public string? ExampleSentence { get; set; }
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
}

public class Question
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "MultipleChoice";
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? Explanation { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = [];
}

public class QuestionOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public class UserCourse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public int? CurrentLessonId { get; set; }
}

public class UserLessonProgress
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public string Status { get; set; } = ProgressStatuses.NotStarted;
    public int Score { get; set; }
    public int XP { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class UserAnswer
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int QuestionId { get; set; }
    public Question? Question { get; set; }
    public string SelectedAnswer { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}

public class Streak
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public DateTime? LastStudyDate { get; set; }
}

public class Achievement
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int RequiredXP { get; set; }
}

public class UserAchievement
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int AchievementId { get; set; }
    public Achievement? Achievement { get; set; }
    public DateTime UnlockedAt { get; set; } = DateTime.UtcNow;
}

public class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string PlanName { get; set; } = "Free";
    public string Status { get; set; } = "Active";
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiredAt { get; set; }
}

public class Payment
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public record DashboardSummary(int XP, int CurrentStreak, int LessonsCompleted, int AverageScore, int Rank);

public record QuizSubmission(int LessonId, IReadOnlyDictionary<int, string> Answers);

public record QuizResult(int Score, int XP, bool Passed, int CorrectAnswers, int TotalQuestions, int? NextLessonId);
