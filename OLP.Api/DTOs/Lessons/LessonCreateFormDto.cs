using Microsoft.AspNetCore.Http;

namespace OLP.Api.DTOs.Lessons
{
    public class LessonCreateFormDto
    {
        public int CourseId { get; set; }                 // you can also pass in route, but keep it for clarity
        public string Title { get; set; } = null!;
        public string? Content { get; set; }

        // Choose: Text / Video / File (or whatever you want)
        public string LessonType { get; set; } = "Text";

        public int OrderIndex { get; set; } = 1;
        public int? EstimatedDuration { get; set; }

        // Optional direct links (if you already have URLs)
        public string? VideoUrl { get; set; }
        public string? AttachmentUrl { get; set; }

        // Optional uploads
        public IFormFile? VideoFile { get; set; }
        public IFormFile? AttachmentFile { get; set; }
    }
}
