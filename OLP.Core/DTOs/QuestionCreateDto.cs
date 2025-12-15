using System.Collections.Generic;

namespace OLP.Core.DTOs
{
    public class QuestionCreateDto
    {
        public int QuizId { get; set; }
        public string QuestionText { get; set; } = null!;
        public string QuestionType { get; set; } = "MCQ";
        public int Points { get; set; } = 1;
        public List<AnswerCreateDto> Answers { get; set; } = new();
    }

    public class AnswerCreateDto
    {
        public string AnswerText { get; set; } = null!;
        public bool IsCorrect { get; set; } = false;
    }
}
