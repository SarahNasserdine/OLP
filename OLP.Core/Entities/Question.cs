using System.Collections.Generic;
using OLP.Core.Enums;

namespace OLP.Core.Entities
{
    public class Question
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string QuestionText { get; set; } = null!;
        public QuestionType QuestionType { get; set; }
        public int Points { get; set; } = 1;
        // NEW FIELD: order index for sorting
        public int OrderIndex { get; set; } = 0;


        // Navigation
        public Quiz Quiz { get; set; } = null!;
        public ICollection<Answer>? Answers { get; set; }
    }
}
