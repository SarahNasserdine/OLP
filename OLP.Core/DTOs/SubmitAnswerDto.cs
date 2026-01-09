using System.Collections.Generic;

namespace OLP.Core.DTOs
{
    public class SubmitAnswerDto
    {
        public int QuestionId { get; set; }

        // MCQ / TF
        public int? AnswerId { get; set; }

        // MSQ
        public List<int>? AnswerIds { get; set; }

        // ShortAnswer
        public string? GivenTextAnswer { get; set; }
    }
}
