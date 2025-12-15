using System;

namespace OLP.Core.Entities
{
    public class QuizAttemptAnswer
    {
        public int Id { get; set; }
        public int QuizAttemptId { get; set; }
        public int QuestionId { get; set; }
        public int? AnswerId { get; set; }
        public string? GivenTextAnswer { get; set; }
        public string? SelectedAnswerIdsJson { get; set; } // for MSQ
        public bool IsCorrect { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public QuizAttempt QuizAttempt { get; set; } = null!;
        public Question Question { get; set; } = null!;
        public Answer? Answer { get; set; }
    }
}
