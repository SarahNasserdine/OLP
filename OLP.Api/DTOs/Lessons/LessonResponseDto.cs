namespace OLP.Api.DTOs.Lessons
{
    public class LessonResponseDto
    {
        public int Id { get; set; }
        public int CourseId { get; set; }

        public string Title { get; set; } = "";
        public string? Content { get; set; }

        public string? VideoUrl { get; set; }
        public string? VideoPublicId { get; set; }
        public string? AttachmentUrl { get; set; }
        public string? AttachmentPublicId { get; set; }

        public string LessonType { get; set; } = "Text";
        public int OrderIndex { get; set; }
        public int? EstimatedDuration { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
