using System;

namespace OLP.Core.Entities
{
    public class LessonCompletion
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public int UserId { get; set; }
        public DateTime CompletedDate { get; set; } = DateTime.UtcNow;

        // Navigation
        public Lesson Lesson { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
