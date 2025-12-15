using System.Collections.Generic;

namespace OLP.Core.DTOs
{
    public class SubmitQuizDto
    {
        public int QuizId { get; set; }
        public List<SubmitAnswerDto> Answers { get; set; } = new();
    }
}
