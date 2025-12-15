using System;

namespace OLP.Core.Entities
{
    public class Enrollment
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public DateTime EnrollDate { get; set; } = DateTime.UtcNow;
        public decimal Progress { get; set; } = 0;

        // Navigation
        public User User { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }
}
