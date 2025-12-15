using System;
using System.Collections.Generic;

namespace OLP.Core.DTOs
{
	public class CourseDto
	{
		public int Id { get; set; }
		public string Title { get; set; } = null!;
		public string? Category { get; set; }
		public string Difficulty { get; set; } = null!;
		public bool IsPublished { get; set; }
		public DateTime CreatedAt { get; set; }

		public List<LessonDto> Lessons { get; set; } = new();
	}
}
