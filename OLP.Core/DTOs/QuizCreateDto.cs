namespace OLP.Core.DTOs
{
    public class QuizCreateDto
    {
        public int CourseId { get; set; }
        public int? LessonId { get; set; }
        public string Title { get; set; } = null!;

        // 👇 Add these missing fields
        public int PassingScore { get; set; }
        public int TimeLimit { get; set; }
        public bool ShuffleQuestions { get; set; }
        public bool AllowRetake { get; set; }
    }
}
