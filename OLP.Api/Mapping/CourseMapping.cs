using OLP.Api.DTOs.Courses;
using OLP.Core.Entities;

namespace OLP.Api.Mapping
{
    public static class CourseMapping
    {
        public static CourseResponseDto ToDto(this Course c)
        {
            return new CourseResponseDto
            {
                Id = c.Id,
                Title = c.Title,
                ShortDescription = c.ShortDescription,
                LongDescription = c.LongDescription,
                Category = c.Category,
                Difficulty = c.Difficulty,
                EstimatedDuration = c.EstimatedDuration,
                ThumbnailUrl = c.ThumbnailUrl,
                ThumbnailPublicId = c.ThumbnailPublicId,
                CreatedById = c.CreatedById,
                CreatorName = c.Creator != null ? c.Creator.FullName : null, // adapt if your User has Name/Email
                IsPublished = c.IsPublished,
                CreatedAt = c.CreatedAt
            };
        }
    }
}
