using LanguageLearning.Domain;

namespace LanguageLearning.Application.Abstractions;

public interface ILearningCatalogService
{
    Task<DashboardSummary> GetDashboardAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Language>> GetLanguagesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Course>> GetCoursesAsync(bool includeDrafts = false, CancellationToken cancellationToken = default);
    Task<Course?> GetCourseAsync(int id, CancellationToken cancellationToken = default);
    Task<Lesson?> GetLessonAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserLessonProgress>> GetProgressAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserAnswer>> GetMistakesAsync(int userId, CancellationToken cancellationToken = default);
    Task<QuizResult> SubmitQuizAsync(int userId, QuizSubmission submission, CancellationToken cancellationToken = default);
}

public interface IAdminLearningService
{
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Course>> GetCoursesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Unit>> GetUnitsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Lesson>> GetLessonsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Vocabulary>> GetVocabularyAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Question>> GetQuestionsAsync(CancellationToken cancellationToken = default);
    Task<Course> SaveCourseAsync(Course course, CancellationToken cancellationToken = default);
    Task<Unit> SaveUnitAsync(Unit unit, CancellationToken cancellationToken = default);
    Task<Lesson> SaveLessonAsync(Lesson lesson, CancellationToken cancellationToken = default);
    Task<Vocabulary> SaveVocabularyAsync(Vocabulary vocabulary, CancellationToken cancellationToken = default);
    Task<Question> SaveQuestionAsync(Question question, CancellationToken cancellationToken = default);
    Task ToggleCoursePublishedAsync(int courseId, CancellationToken cancellationToken = default);
}

public interface IAuthService
{
    Task<User?> ValidateUserAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<User?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User> RegisterAsync(string fullName, string email, string password, string learningGoal, CancellationToken cancellationToken = default);
    Task<AuthSession?> LoginAsync(string email, string password, DeviceInfo device, CancellationToken cancellationToken = default);
    Task LogoutAsync(int userId, string sessionToken, CancellationToken cancellationToken = default);
    Task<bool> IsSessionActiveAsync(int userId, string sessionToken, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserSession>> GetSessionsAsync(CancellationToken cancellationToken = default);
}

public interface IAIScoringService
{
    Task<AIScoringResponse> ScoreAsync(
        string sentence,
        string grammarStructure,
        IReadOnlyList<string> vocabulary,
        CancellationToken cancellationToken = default);
}

public interface ISentencePracticeService
{
    Task<AIScoringResult> SubmitAsync(
        SentenceScoringRequest request,
        CancellationToken cancellationToken = default);
}
