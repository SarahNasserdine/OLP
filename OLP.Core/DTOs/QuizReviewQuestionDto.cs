namespace OLP.Core.DTOs
{
    public class QuizReviewQuestionDto
    {
        public string QuestionText { get; set; } = null!;
        public string? GivenTextAnswer { get; set; }
        public string? CorrectAnswer { get; set; }
        public bool IsCorrect { get; set; }
    }
}
