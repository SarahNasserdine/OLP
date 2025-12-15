using System.Collections.Generic;

namespace OLP.Core.Entities
{
    public class Quiz
    {
        public int Id { get; set; }
        public int CourseId { get; set; }
        public int? LessonId { get; set; }
        public string Title { get; set; } = null!;
        public int PassingScore { get; set; }
        public int? TimeLimit { get; set; }
        public bool ShuffleQuestions { get; set; } = false;
        public bool AllowRetake { get; set; } = false;
        public bool IsActive { get; set; } = true;

        // Navigation
        public Course Course { get; set; } = null!;
        public Lesson? Lesson { get; set; }
        public ICollection<Question>? Questions { get; set; }
        public ICollection<QuizAttempt>? QuizAttempts { get; set; }
    }
}
