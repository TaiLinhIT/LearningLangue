using LanguageLearning.Domain;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Data;

public class LanguageLearningDbContext(DbContextOptions<LanguageLearningDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<LessonStep> LessonSteps => Set<LessonStep>();
    public DbSet<Vocabulary> Vocabulary => Set<Vocabulary>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<UserCourse> UserCourses => Set<UserCourse>();
    public DbSet<UserLessonProgress> UserLessonProgress => Set<UserLessonProgress>();
    public DbSet<StudentStepProgress> StudentStepProgress => Set<StudentStepProgress>();
    public DbSet<SentencePractice> SentencePractices => Set<SentencePractice>();
    public DbSet<AIScoringResult> AIScoringResults => Set<AIScoringResult>();
    public DbSet<UserAnswer> UserAnswers => Set<UserAnswer>();
    public DbSet<Streak> Streaks => Set<Streak>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<User>().Property(x => x.IsActive).HasDefaultValue(true);
        modelBuilder.Entity<UserSession>().HasIndex(x => x.SessionToken).IsUnique();
        modelBuilder.Entity<UserSession>()
            .HasOne(x => x.User)
            .WithMany(x => x.Sessions)
            .HasForeignKey(x => x.UserId);
        modelBuilder.Entity<LoginHistory>()
            .HasOne(x => x.User)
            .WithMany(x => x.LoginHistory)
            .HasForeignKey(x => x.UserId);
        modelBuilder.Entity<SentencePractice>()
            .HasOne(x => x.ScoringResult)
            .WithOne(x => x.SentencePractice)
            .HasForeignKey<AIScoringResult>(x => x.SentencePracticeId);
        modelBuilder.Entity<Language>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<Course>().Property(x => x.Title).HasMaxLength(180);
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Language>().HasData(
            new Language { Id = 1, Name = "English", Code = "en", FlagIcon = "US" },
            new Language { Id = 2, Name = "Japanese", Code = "ja", FlagIcon = "JP" },
            new Language { Id = 3, Name = "Korean", Code = "ko", FlagIcon = "KR" });

        modelBuilder.Entity<Course>().HasData(
            new Course { Id = 1, LanguageId = 1, Title = "English Starter Path", Description = "Build daily conversation, core vocabulary, and confident pronunciation.", Level = "A1", ThumbnailUrl = "https://images.unsplash.com/photo-1522202176988-66273c2fd55f?auto=format&fit=crop&w=900&q=80", IsPublished = true },
            new Course { Id = 2, LanguageId = 1, Title = "Workplace English", Description = "Meetings, emails, presentations, and polite office conversation.", Level = "A2", ThumbnailUrl = "https://images.unsplash.com/photo-1552664730-d307ca884978?auto=format&fit=crop&w=900&q=80", IsPublished = true },
            new Course { Id = 3, LanguageId = 2, Title = "Japanese First Steps", Description = "Hiragana, greetings, and survival phrases for beginners.", Level = "A1", ThumbnailUrl = "https://images.unsplash.com/photo-1493976040374-85c8e12f0c0e?auto=format&fit=crop&w=900&q=80", IsPublished = true });

        modelBuilder.Entity<Unit>().HasData(
            new Unit { Id = 1, CourseId = 1, Title = "Meet People", SortOrder = 1 },
            new Unit { Id = 2, CourseId = 1, Title = "Daily Life", SortOrder = 2 },
            new Unit { Id = 3, CourseId = 2, Title = "Office Basics", SortOrder = 1 },
            new Unit { Id = 4, CourseId = 3, Title = "Kana Basics", SortOrder = 1 });

        modelBuilder.Entity<Lesson>().HasData(
            new Lesson { Id = 1, UnitId = 1, Title = "Greetings and names", Description = "Say hello, introduce yourself, and answer simple questions.", LessonType = LessonTypes.Vocabulary, XPReward = 20, SortOrder = 1, IsLocked = false },
            new Lesson { Id = 2, UnitId = 1, Title = "Where are you from?", Description = "Talk about countries, cities, and origin.", LessonType = LessonTypes.Speaking, XPReward = 25, SortOrder = 2, IsLocked = true },
            new Lesson { Id = 3, UnitId = 2, Title = "Food and drinks", Description = "Order simple meals and describe preferences.", LessonType = LessonTypes.Listening, XPReward = 25, SortOrder = 1, IsLocked = true },
            new Lesson { Id = 4, UnitId = 3, Title = "Meetings at work", Description = "Understand schedules, reports, and meeting language.", LessonType = LessonTypes.Grammar, XPReward = 30, SortOrder = 1, IsLocked = false },
            new Lesson { Id = 5, UnitId = 4, Title = "Hiragana vowels", Description = "Read and recognize the first Japanese sounds.", LessonType = LessonTypes.Vocabulary, XPReward = 20, SortOrder = 1, IsLocked = false });

        modelBuilder.Entity<LessonStep>().HasData(
            new LessonStep { Id = 1, LessonId = 1, StepType = LessonStepTypes.Video, Title = "Video gioi thieu", Description = "Xem video va hoc tu vung trong ngu canh.", SortOrder = 1, MinScoreToPass = 0 },
            new LessonStep { Id = 2, LessonId = 1, StepType = LessonStepTypes.Flashcards, Title = "Flashcard tu vung", Description = "On tap toan bo tu moi cua bai.", SortOrder = 2, MinScoreToPass = 0 },
            new LessonStep { Id = 3, LessonId = 1, StepType = LessonStepTypes.Grammar, Title = "Cau truc ngu phap", Description = "Hoc mau cau gioi thieu ban than.", SortOrder = 3, MinScoreToPass = 0 },
            new LessonStep { Id = 4, LessonId = 1, StepType = LessonStepTypes.Quiz, Title = "Quiz", Description = "Dat it nhat 70 diem de tiep tuc.", SortOrder = 4, MinScoreToPass = 70 },
            new LessonStep { Id = 5, LessonId = 1, StepType = LessonStepTypes.Exercise, Title = "Bai tap van dung", Description = "Van dung tu vung va ngu phap.", SortOrder = 5, MinScoreToPass = 70 },
            new LessonStep { Id = 6, LessonId = 1, StepType = LessonStepTypes.SentencePractice, Title = "Tu dat cau", Description = "Gui cau cua ban de AI cham diem.", SortOrder = 6, MinScoreToPass = 70 },
            new LessonStep { Id = 7, LessonId = 1, StepType = LessonStepTypes.Result, Title = "Ket qua", Description = "Tong ket diem, XP va tien do.", SortOrder = 7, MinScoreToPass = 0 });

        modelBuilder.Entity<Vocabulary>().HasData(
            new Vocabulary { Id = 1, LessonId = 1, Word = "hello", Meaning = "xin chao", Pronunciation = "heh-low", ExampleSentence = "Hello, my name is Linh." },
            new Vocabulary { Id = 2, LessonId = 1, Word = "name", Meaning = "ten", Pronunciation = "naym", ExampleSentence = "What is your name?" },
            new Vocabulary { Id = 3, LessonId = 2, Word = "from", Meaning = "den tu", Pronunciation = "frum", ExampleSentence = "I am from Vietnam." },
            new Vocabulary { Id = 4, LessonId = 4, Word = "meeting", Meaning = "cuoc hop", Pronunciation = "mee-ting", ExampleSentence = "The meeting starts at nine." },
            new Vocabulary { Id = 5, LessonId = 5, Word = "a", Meaning = "am a trong hiragana", Pronunciation = "a", ExampleSentence = "A sounds like ah." });

        modelBuilder.Entity<Question>().HasData(
            new Question { Id = 1, LessonId = 1, QuestionText = "Which sentence introduces your name?", QuestionType = "MultipleChoice", CorrectAnswer = "My name is An.", Explanation = "Use 'My name is...' for a natural self-introduction." },
            new Question { Id = 2, LessonId = 1, QuestionText = "What does 'Nice to meet you' mean?", QuestionType = "MultipleChoice", CorrectAnswer = "Rat vui duoc gap ban", Explanation = "It is a polite phrase for first meetings." },
            new Question { Id = 3, LessonId = 2, QuestionText = "Choose the correct answer: Where are you from?", QuestionType = "Speaking", CorrectAnswer = "I am from Vietnam.", Explanation = "Use 'I am from...' plus a place." },
            new Question { Id = 4, LessonId = 4, QuestionText = "What is a deadline?", QuestionType = "Grammar", CorrectAnswer = "Han chot", Explanation = "A deadline is the latest time work should be finished." });

        modelBuilder.Entity<QuestionOption>().HasData(
            new QuestionOption { Id = 1, QuestionId = 1, OptionText = "I am coffee.", IsCorrect = false },
            new QuestionOption { Id = 2, QuestionId = 1, OptionText = "My name is An.", IsCorrect = true },
            new QuestionOption { Id = 3, QuestionId = 1, OptionText = "She from Japan.", IsCorrect = false },
            new QuestionOption { Id = 4, QuestionId = 2, OptionText = "Tam biet", IsCorrect = false },
            new QuestionOption { Id = 5, QuestionId = 2, OptionText = "Rat vui duoc gap ban", IsCorrect = true },
            new QuestionOption { Id = 6, QuestionId = 2, OptionText = "Toi dang hoc", IsCorrect = false },
            new QuestionOption { Id = 7, QuestionId = 3, OptionText = "I am from Vietnam.", IsCorrect = true },
            new QuestionOption { Id = 8, QuestionId = 3, OptionText = "I from Vietnam.", IsCorrect = false },
            new QuestionOption { Id = 9, QuestionId = 3, OptionText = "Vietnam from I.", IsCorrect = false },
            new QuestionOption { Id = 10, QuestionId = 4, OptionText = "Phong hop", IsCorrect = false },
            new QuestionOption { Id = 11, QuestionId = 4, OptionText = "Han chot", IsCorrect = true },
            new QuestionOption { Id = 12, QuestionId = 4, OptionText = "Ngay nghi", IsCorrect = false });

        modelBuilder.Entity<Achievement>().HasData(
            new Achievement { Id = 1, Name = "First Spark", Description = "Earn your first 50 XP.", IconUrl = "spark", RequiredXP = 50 },
            new Achievement { Id = 2, Name = "Week Rhythm", Description = "Keep a 7 day streak.", IconUrl = "calendar", RequiredXP = 150 },
            new Achievement { Id = 3, Name = "Quiz Ace", Description = "Pass 10 quizzes.", IconUrl = "trophy", RequiredXP = 300 });
    }
}
