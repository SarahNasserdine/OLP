namespace OLP.Core.DTOs
{
    public class SubmitAnswerDto
    {
        public int QuestionId { get; set; }

        // for multiple choice / true-false questions
        public int? AnswerId { get; set; }

        // for short answer questions
        public string? GivenTextAnswer { get; set; }
    }
}
