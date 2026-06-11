using LanguageLearning.Application.Abstractions;
using LanguageLearning.Domain;
using LanguageLearning.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Infrastructure.Services;

public sealed class MockAIScoringService : IAIScoringService
{
    public Task<AIScoringResponse> ScoreAsync(
        string sentence,
        string grammarStructure,
        IReadOnlyList<string> vocabulary,
        CancellationToken cancellationToken = default)
    {
        var trimmed = sentence.Trim();
        var hasTerminalPunctuation = trimmed.EndsWith('.') || trimmed.EndsWith('!') || trimmed.EndsWith('?');
        var wordCount = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
        var vocabularyHits = vocabulary.Count(word =>
            trimmed.Contains(word, StringComparison.OrdinalIgnoreCase));

        var grammarScore = Math.Clamp(45 + (wordCount * 5) + (hasTerminalPunctuation ? 10 : 0), 0, 100);
        var vocabularyScore = vocabulary.Count == 0
            ? Math.Clamp(50 + (wordCount * 4), 0, 100)
            : Math.Clamp(40 + (vocabularyHits * 20), 0, 100);
        var naturalnessScore = Math.Clamp(40 + (wordCount * 6) + (hasTerminalPunctuation ? 8 : 0), 0, 100);
        var overallScore = (int)Math.Round((grammarScore + vocabularyScore + naturalnessScore) / 3m);
        var feedback = overallScore >= 70
            ? "Cau cua ban ro rang va dung muc tieu. Hay thu them mot chi tiet de cau tu nhien hon."
            : "Cau da dung huong, nhung can du chu ngu, dong tu va dau cau. Hay viet mot cau day du hon.";
        var suggestion = hasTerminalPunctuation ? trimmed : $"{trimmed}.";

        return Task.FromResult(new AIScoringResponse(
            grammarScore,
            vocabularyScore,
            naturalnessScore,
            overallScore,
            feedback,
            suggestion));
    }
}

public sealed class SentencePracticeService(
    LanguageLearningDbContext db,
    IAIScoringService scoringService) : ISentencePracticeService
{
    public async Task<AIScoringResult> SubmitAsync(
        SentenceScoringRequest request,
        CancellationToken cancellationToken = default)
    {
        var score = await scoringService.ScoreAsync(
            request.Sentence,
            request.GrammarStructure,
            request.Vocabulary,
            cancellationToken);

        var practice = new SentencePractice
        {
            UserId = request.UserId,
            LessonStepId = request.LessonStepId,
            GrammarStructure = request.GrammarStructure,
            StudentSentence = request.Sentence.Trim()
        };
        var result = new AIScoringResult
        {
            SentencePractice = practice,
            GrammarScore = score.GrammarScore,
            VocabularyScore = score.VocabularyScore,
            NaturalnessScore = score.NaturalnessScore,
            OverallScore = score.OverallScore,
            Feedback = score.Feedback,
            SuggestedSentence = score.SuggestedSentence
        };

        db.AIScoringResults.Add(result);
        await db.SaveChangesAsync(cancellationToken);
        return result;
    }

    public Task<AIScoringResult?> GetResultAsync(
        int sentencePracticeId,
        int userId,
        CancellationToken cancellationToken = default) =>
        db.AIScoringResults
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.SentencePracticeId == sentencePracticeId
                    && x.SentencePractice!.UserId == userId,
                cancellationToken);
}
