using System;
using System.Collections.Generic;

namespace OLP.Core.DTOs
{
    public class QuizReviewDto
    {
        public int AttemptId { get; set; }
        public int QuizId { get; set; }
        public int Score { get; set; }
        public int AttemptNumber { get; set; }
        public DateTime AttemptDate { get; set; }
        public List<QuizReviewQuestionDto> Questions { get; set; } = new();
    }

    public class QuizReviewQuestionDto
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = "";
        public int Points { get; set; }
        public bool IsCorrect { get; set; }

        // student answers
        public int? SelectedAnswerId { get; set; }              // MCQ/TF
        public List<int> SelectedAnswerIds { get; set; } = new(); // MSQ
        public string? GivenTextAnswer { get; set; }            // ShortAnswer

        // correct answers
        public List<int> CorrectAnswerIds { get; set; } = new();

        // all answers (optional)
        public List<QuizReviewAnswerDto> Answers { get; set; } = new();
    }

    public class QuizReviewAnswerDto
    {
        public int AnswerId { get; set; }
        public string AnswerText { get; set; } = "";
        public bool IsCorrect { get; set; }
    }
}
