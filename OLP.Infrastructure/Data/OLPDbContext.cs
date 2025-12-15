using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;

namespace OLP.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // === DbSets ===
        public DbSet<User> Users { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Lesson> Lessons { get; set; }
        public DbSet<Enrollment> Enrollments { get; set; }
        public DbSet<LessonCompletion> LessonCompletions { get; set; }
        public DbSet<Quiz> Quizzes { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<QuizAttempt> QuizAttempts { get; set; }
        public DbSet<QuizAttemptAnswer> QuizAttemptAnswers { get; set; }
        public DbSet<Certificate> Certificates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== ENUM CONVERSIONS =====
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<int>();

            modelBuilder.Entity<Course>()
                .Property(c => c.Difficulty)
                .HasConversion<int>();

            modelBuilder.Entity<Question>()
                .Property(q => q.QuestionType)
                .HasConversion<int>();

            // ===== USER ↔ COURSE =====
            modelBuilder.Entity<Course>()
                .HasOne(c => c.Creator)
                .WithMany(u => u.CoursesCreated)
                .HasForeignKey(c => c.CreatedById)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== COURSE ↔ LESSON =====
            modelBuilder.Entity<Lesson>()
                .HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== USER ↔ ENROLLMENT =====
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.User)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ===== COURSE ↔ ENROLLMENT =====
            modelBuilder.Entity<Enrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== LESSON ↔ LESSON COMPLETION =====
            modelBuilder.Entity<LessonCompletion>()
                .HasOne(lc => lc.Lesson)
                .WithMany(l => l.LessonCompletions)
                .HasForeignKey(lc => lc.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LessonCompletion>()
                .HasOne(lc => lc.User)
                .WithMany(u => u.LessonCompletions)
                .HasForeignKey(lc => lc.UserId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ fixed

            // ===== QUIZ ↔ COURSE =====
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Course)
                .WithMany(c => c.Quizzes)
                .HasForeignKey(q => q.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== QUIZ ↔ LESSON =====
            modelBuilder.Entity<Quiz>()
                .HasOne(q => q.Lesson)
                .WithMany(l => l.Quizzes)
                .HasForeignKey(q => q.LessonId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ fixed

            // ===== QUESTION ↔ QUIZ =====
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Quiz)
                .WithMany(z => z.Questions)
                .HasForeignKey(q => q.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== ANSWER ↔ QUESTION =====
            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== QUIZ ATTEMPT ↔ QUIZ =====
            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.Quiz)
                .WithMany(q => q.QuizAttempts)
                .HasForeignKey(qa => qa.QuizId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== QUIZ ATTEMPT ↔ USER =====
            modelBuilder.Entity<QuizAttempt>()
                .HasOne(qa => qa.User)
                .WithMany(u => u.QuizAttempts)
                .HasForeignKey(qa => qa.UserId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ prevent cascade chain

            // ===== QUIZ ATTEMPT ANSWERS =====
            modelBuilder.Entity<QuizAttemptAnswer>()
                .HasOne(qa => qa.QuizAttempt)
                .WithMany(a => a.Answers)
                .HasForeignKey(qa => qa.QuizAttemptId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ FINAL FIX

            modelBuilder.Entity<QuizAttemptAnswer>()
                .HasOne(qa => qa.Question)
                .WithMany()
                .HasForeignKey(qa => qa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict); // ✅ prevent multiple cascade paths

            modelBuilder.Entity<QuizAttemptAnswer>()
                .HasOne(qa => qa.Answer)
                .WithMany()
                .HasForeignKey(qa => qa.AnswerId)
                .OnDelete(DeleteBehavior.SetNull);

            // ===== CERTIFICATE =====
            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.User)
                .WithMany(u => u.Certificates)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Certificate>()
                .HasOne(c => c.Course)
                .WithMany(ca => ca.Certificates)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // ===== ENROLLMENT PRECISION =====
            modelBuilder.Entity<Enrollment>()
                .Property(e => e.Progress)
                .HasPrecision(18, 2);

            // ===== UNIQUE CONSTRAINTS =====
            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.UserId, e.CourseId })
                .IsUnique();

            modelBuilder.Entity<LessonCompletion>()
                .HasIndex(lc => new { lc.UserId, lc.LessonId })
                .IsUnique();

            modelBuilder.Entity<Certificate>()
                .HasIndex(c => c.VerificationCode)
                .IsUnique();
        }
    }
}
