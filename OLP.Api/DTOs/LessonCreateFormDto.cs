using Microsoft.AspNetCore.Http;

namespace OLP.Api.DTOs
{
    public class LessonCreateFormDto
    {
        public string Title { get; set; } = null!;
        public string LessonType { get; set; } = "Text"; // Text, Video, Pdf
        public string? Content { get; set; }             // for Text lessons

        public int OrderIndex { get; set; }
        public int? EstimatedDuration { get; set; }

        // ✅ file from browser (pdf or mp4)
        public IFormFile? File { get; set; }
    }
}
