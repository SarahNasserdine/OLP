using System;

namespace OLP.Core.Entities
{
    public class Certificate
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int CourseId { get; set; }
        public string DownloadUrl { get; set; } = null!;
        public string? VerificationCode { get; set; }
        public string Status { get; set; } = "Generated";
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public User User { get; set; } = null!;
        public Course Course { get; set; } = null!;
    }
}
