namespace OLP.Core.DTOs
{
    public class LessonCreateDto
    {
        public int CourseId { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public string? VideoUrl { get; set; }
        public string? AttachmentUrl { get; set; }
        public int OrderIndex { get; set; }
        public int? EstimatedDuration { get; set; }
    }
}
