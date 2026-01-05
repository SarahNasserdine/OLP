using Microsoft.AspNetCore.Http;

namespace OLP.Api.DTOs
{
	public class CourseCreateFormDto
	{
		public string Title { get; set; } = null!;
		public string? ShortDescription { get; set; }
		public string? LongDescription { get; set; }
		public string? Category { get; set; }
		public string Difficulty { get; set; } = "Beginner";
		public int? EstimatedDuration { get; set; }

		// ✅ file chosen from browser / swagger
		public IFormFile? ThumbnailFile { get; set; }
	}
}
