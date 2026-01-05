using System;
using System.Collections.Generic;
using OLP.Core.Enums;

namespace OLP.Core.Entities
{
    public class Course
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string ShortDescription { get; set; } = null!;
        public string LongDescription { get; set; } = null!;
        public string Category { get; set; } = null!;
        public DifficultyLevel Difficulty { get; set; }
        public int? EstimatedDuration { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ThumbnailPublicId { get; set; }


        // ✅ Add these two properties:
        public int CreatedById { get; set; }     // foreign key to User
        public User Creator { get; set; } = null!; // navigation property

        public bool IsPublished { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation collections
        public ICollection<Lesson>? Lessons { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<Quiz>? Quizzes { get; set; }
        public ICollection<Certificate>? Certificates { get; set; }
    }
}
