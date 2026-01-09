using System;

namespace OLP.Api.DTOs.Enrollments
{
    public class EnrollmentResponseDto
    {
        public int Id { get; set; }

        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = "";
        public string? CourseThumbnailUrl { get; set; }

        public int UserId { get; set; }
        public string UserFullName { get; set; } = "";
        public string UserEmail { get; set; } = "";

        public DateTime EnrollDate { get; set; }
        public decimal Progress { get; set; }
        public int? LastAccessedLessonId { get; set; }
        public DateTime? LastAccessedAt { get; set; }
    }
}
