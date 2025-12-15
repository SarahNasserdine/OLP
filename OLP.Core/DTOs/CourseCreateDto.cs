namespace OLP.Core.DTOs
{
    public class CourseCreateDto
    {
        public string Title { get; set; } = null!;
        public string? ShortDescription { get; set; }
        public string? LongDescription { get; set; }
        public string? Category { get; set; }
        public string Difficulty { get; set; } = "Beginner";
        public int? EstimatedDuration { get; set; }
        public string? Thumbnail { get; set; }
    }
}
