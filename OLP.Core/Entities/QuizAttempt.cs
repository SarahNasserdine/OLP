using System;
using System.Collections.Generic;

namespace OLP.Core.Entities
{
    public class QuizAttempt
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int UserId { get; set; }
        public int Score { get; set; }
        public int AttemptNumber { get; set; } = 1;
        public DateTime AttemptDate { get; set; } = DateTime.UtcNow;
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }

        // Navigation
        public Quiz Quiz { get; set; } = null!;
        public User User { get; set; } = null!;
        public ICollection<QuizAttemptAnswer>? Answers { get; set; }
    }
}
