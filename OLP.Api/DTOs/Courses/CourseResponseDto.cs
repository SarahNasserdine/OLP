using OLP.Core.Enums;

namespace OLP.Api.DTOs.Courses
{
    public class CourseResponseDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string ShortDescription { get; set; } = "";
        public string LongDescription { get; set; } = "";
        public string Category { get; set; } = "";
        public DifficultyLevel Difficulty { get; set; }
        public int? EstimatedDuration { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? ThumbnailPublicId { get; set; }
        public int CreatedById { get; set; }
        public string? CreatorName { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
