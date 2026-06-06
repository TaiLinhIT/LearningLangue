using LanguageLearning.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LanguageLearning.Persistence.Data;

public class LearningDbContext(DbContextOptions<LearningDbContext> options) : DbContext(options)
{
    public DbSet<LearningLanguageEntity> Languages => Set<LearningLanguageEntity>();

    public DbSet<LessonEntity> Lessons => Set<LessonEntity>();

    public DbSet<PracticeQuestionEntity> PracticeQuestions => Set<PracticeQuestionEntity>();

    public DbSet<VocabularyItemEntity> VocabularyItems => Set<VocabularyItemEntity>();

    public DbSet<PlacementQuestionEntity> PlacementQuestions => Set<PlacementQuestionEntity>();

    public DbSet<LearningProgressEntity> LearningProgress => Set<LearningProgressEntity>();

    public DbSet<SkillProgressEntity> SkillProgress => Set<SkillProgressEntity>();

    public DbSet<PricingPlanEntity> PricingPlans => Set<PricingPlanEntity>();

    public DbSet<AdminMetricEntity> AdminMetrics => Set<AdminMetricEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LearningLanguageEntity>(entity =>
        {
            entity.ToTable("LearningLanguages");
            entity.HasKey(language => language.Code);
            entity.Property(language => language.Code).HasMaxLength(12);
            entity.Property(language => language.Name).HasMaxLength(120);
            entity.Property(language => language.NativeName).HasMaxLength(120);
            entity.Property(language => language.Flag).HasMaxLength(12);
            entity.Property(language => language.Difficulty).HasMaxLength(60);
        });

        modelBuilder.Entity<LessonEntity>(entity =>
        {
            entity.ToTable("Lessons");
            entity.HasKey(lesson => lesson.Id);
            entity.Property(lesson => lesson.Title).HasMaxLength(220);
            entity.Property(lesson => lesson.LanguageCode).HasMaxLength(12);
            entity.Property(lesson => lesson.Level).HasMaxLength(20);
            entity.Property(lesson => lesson.Topic).HasMaxLength(120);
            entity.Property(lesson => lesson.Skill).HasMaxLength(80);
            entity.HasOne(lesson => lesson.Language)
                .WithMany()
                .HasForeignKey(lesson => lesson.LanguageCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PracticeQuestionEntity>(entity =>
        {
            entity.ToTable("PracticeQuestions");
            entity.HasKey(question => question.Id);
            entity.Property(question => question.Prompt).HasMaxLength(500);
            entity.HasOne(question => question.Lesson)
                .WithMany(lesson => lesson.Questions)
                .HasForeignKey(question => question.LessonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VocabularyItemEntity>(entity =>
        {
            entity.ToTable("VocabularyItems");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Term).HasMaxLength(160);
            entity.Property(item => item.Meaning).HasMaxLength(220);
            entity.Property(item => item.Phonetic).HasMaxLength(160);
            entity.Property(item => item.LanguageCode).HasMaxLength(12);
            entity.Property(item => item.Topic).HasMaxLength(120);
            entity.HasOne(item => item.Language)
                .WithMany()
                .HasForeignKey(item => item.LanguageCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PlacementQuestionEntity>(entity =>
        {
            entity.ToTable("PlacementQuestions");
            entity.HasKey(question => question.Id);
            entity.Property(question => question.Skill).HasMaxLength(80);
        });

        modelBuilder.Entity<LearningProgressEntity>(entity =>
        {
            entity.ToTable("LearningProgress");
            entity.HasKey(progress => progress.Id);
            entity.Property(progress => progress.CurrentLevel).HasMaxLength(20);
        });

        modelBuilder.Entity<SkillProgressEntity>(entity =>
        {
            entity.ToTable("SkillProgress");
            entity.HasKey(skill => skill.Id);
            entity.Property(skill => skill.Name).HasMaxLength(120);
            entity.Property(skill => skill.Icon).HasMaxLength(40);
            entity.HasOne(skill => skill.LearningProgress)
                .WithMany(progress => progress.Skills)
                .HasForeignKey(skill => skill.LearningProgressId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PricingPlanEntity>(entity =>
        {
            entity.ToTable("PricingPlans");
            entity.HasKey(plan => plan.Id);
            entity.Property(plan => plan.Name).HasMaxLength(120);
            entity.Property(plan => plan.Price).HasMaxLength(80);
        });

        modelBuilder.Entity<AdminMetricEntity>(entity =>
        {
            entity.ToTable("AdminMetrics");
            entity.HasKey(metric => metric.Id);
            entity.Property(metric => metric.Label).HasMaxLength(120);
            entity.Property(metric => metric.Value).HasMaxLength(80);
            entity.Property(metric => metric.Change).HasMaxLength(120);
        });
    }
}
