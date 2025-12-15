using System;
using System.Collections.Generic;

namespace OLP.Core.DTOs
{
    public class QuizReviewDto
    {
        public string QuizTitle { get; set; } = null!;
        public int Score { get; set; }
        public DateTime AttemptDate { get; set; }
        public List<QuizReviewQuestionDto> Questions { get; set; } = new();
    }
}
