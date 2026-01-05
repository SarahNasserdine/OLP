using System;
using System.Collections.Generic;
using OLP.Core.Enums;

namespace OLP.Core.Entities
{
    public class User
    {
        public int Id { get; set; }

        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;

        // SECURITY (never return in API responses)
        public string PasswordHash { get; set; } = null!;
        public string? PasswordSalt { get; set; }

        public UserRole Role { get; set; } = UserRole.Student;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsEmailConfirmed { get; set; } = false;
        public string? GoogleId { get; set; }

        // ? NEW (REQUIRED)
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; } = false;

        // Navigation
        public ICollection<Course>? CoursesCreated { get; set; }
        public ICollection<Enrollment>? Enrollments { get; set; }
        public ICollection<LessonCompletion>? LessonCompletions { get; set; }
        public ICollection<QuizAttempt>? QuizAttempts { get; set; }
        public ICollection<Certificate>? Certificates { get; set; }
    }
}
