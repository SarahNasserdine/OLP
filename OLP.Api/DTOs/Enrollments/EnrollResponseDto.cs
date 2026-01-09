using System;

namespace OLP.Api.DTOs.Enrollments
{
    public class EnrollResponseDto
    {
        public int EnrollmentId { get; set; }
        public int CourseId { get; set; }
        public int UserId { get; set; }
        public DateTime EnrollDate { get; set; }
        public string Message { get; set; } = "Enrolled successfully.";
    }
}
