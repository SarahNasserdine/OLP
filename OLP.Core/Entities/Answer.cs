namespace OLP.Core.Entities
{
    public class Answer
    {
        public int Id { get; set; }
        public int QuestionId { get; set; }
        public string AnswerText { get; set; } = null!;
        public bool IsCorrect { get; set; } = false;
        public int? OrderIndex { get; set; }

        // Navigation
        public Question Question { get; set; } = null!;
    }
}
