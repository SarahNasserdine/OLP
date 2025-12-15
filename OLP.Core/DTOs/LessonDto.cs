using System;

namespace OLP.Core.DTOs
{
	public class LessonDto
	{
		public int Id { get; set; }
		public string Title { get; set; } = null!;
		public string? VideoUrl { get; set; }
		public string? Content { get; set; }
		public int? EstimatedDuration { get; set; }
		public DateTime CreatedAt { get; set; }
	}
}
