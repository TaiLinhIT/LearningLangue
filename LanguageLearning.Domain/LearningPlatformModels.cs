namespace LanguageLearning.Domain;

public class CourseEnrollment
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public int StudentId { get; set; }
    public User? Student { get; set; }
    public int? ClassId { get; set; }
    public LearningClass? Class { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}

public class LessonVideo
{
    public int Id { get; set; }
    public int LessonStepId { get; set; }
    public LessonStep? LessonStep { get; set; }
    public string Title { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string? SubtitleUrl { get; set; }
    public int DurationSeconds { get; set; }
    public int RequiredWatchPercent { get; set; } = 80;
    public string? TranscriptText { get; set; }
}

public class Flashcard
{
    public int Id { get; set; }
    public int VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }
    public string FrontText { get; set; } = string.Empty;
    public string BackText { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public int SortOrder { get; set; }
}

public class GrammarStructure
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public string Title { get; set; } = string.Empty;
    public string StructurePattern { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string ExampleSentence { get; set; } = string.Empty;
    public string VietnameseMeaning { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class StudentVocabulary
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User? Student { get; set; }
    public int VocabularyId { get; set; }
    public Vocabulary? Vocabulary { get; set; }
    public string Status { get; set; } = "New";
    public int ReviewCount { get; set; }
    public DateTime? LastReviewedAt { get; set; }
}

public class LearningClass
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Level { get; set; } = "A1";
    public int? TeacherId { get; set; }
    public User? Teacher { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ClassStudent> Students { get; set; } = [];
    public ICollection<ClassComment> Comments { get; set; } = [];
}

public class ClassStudent
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public LearningClass? Class { get; set; }
    public int StudentId { get; set; }
    public User? Student { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public class ClassComment
{
    public int Id { get; set; }
    public int ClassId { get; set; }
    public LearningClass? Class { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsPinned { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<ClassCommentReply> Replies { get; set; } = [];
}

public class ClassCommentReply
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public ClassComment? Comment { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RankingPoint
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User? Student { get; set; }
    public int? ClassId { get; set; }
    public LearningClass? Class { get; set; }
    public int? CourseId { get; set; }
    public Course? Course { get; set; }
    public int XP { get; set; }
    public int QuizScore { get; set; }
    public int AIPracticeScore { get; set; }
    public int CompletionPoint { get; set; }
    public int TotalPoint { get; set; }
    public string RankingScope { get; set; } = "Course";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Reward
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string RewardType { get; set; } = "Achievement";
    public int? RequiredRank { get; set; }
    public int? RequiredPoint { get; set; }
    public int? CourseId { get; set; }
    public Course? Course { get; set; }
    public int? ClassId { get; set; }
    public LearningClass? Class { get; set; }
    public bool IsActive { get; set; } = true;
}

public class StudentReward
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public User? Student { get; set; }
    public int RewardId { get; set; }
    public Reward? Reward { get; set; }
    public string Status { get; set; } = "Eligible";
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClaimedAt { get; set; }
}

public class Notification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "General";
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class IPASound
{
    public int Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string SoundType { get; set; } = "Vowel";
    public string ExampleWord { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string VietnameseGuide { get; set; } = string.Empty;
    public string? ComparisonNote { get; set; }
    public int SortOrder { get; set; }
}

public class IPALesson
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SoundType { get; set; } = "Vowel";
    public int SortOrder { get; set; }
    public ICollection<IPAExercise> Exercises { get; set; } = [];
}

public class IPAExercise
{
    public int Id { get; set; }
    public int IPALessonId { get; set; }
    public IPALesson? IPALesson { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? AudioUrl { get; set; }
    public string Explanation { get; set; } = string.Empty;
}

public class Report
{
    public int Id { get; set; }
    public int ReporterId { get; set; }
    public User? Reporter { get; set; }
    public string TargetType { get; set; } = string.Empty;
    public int TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
