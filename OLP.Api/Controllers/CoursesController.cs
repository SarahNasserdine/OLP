using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using OLP.Api.DTOs;                 // CourseCreateFormDto
using OLP.Api.DTOs.Courses;         // CourseResponseDto  (create this)
using OLP.Api.Mapping;              // CourseMapping.ToDto() (create this)

using OLP.Core.Entities;
using OLP.Core.Enums;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Services;
using OLP.Api.Services;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController : ControllerBase
    {
        private readonly ICourseRepository _courseRepo;
        private readonly CourseService _courseService;
        private readonly ICloudinaryService _cloud;

        public CoursesController(
            ICourseRepository courseRepo,
            CourseService courseService,
            ICloudinaryService cloud)
        {
            _courseRepo = courseRepo;
            _courseService = courseService;
            _cloud = cloud;
        }

        // ✅ GET: api/courses
        // Returns DTOs to avoid object cycles
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IEnumerable<CourseResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] string? q, [FromQuery] string? category, [FromQuery] string? difficulty, [FromQuery] string? sort)
        {
            DifficultyLevel? parsedDifficulty = null;
            if (!string.IsNullOrWhiteSpace(difficulty))
            {
                if (!Enum.TryParse<DifficultyLevel>(difficulty, true, out var parsed))
                    return BadRequest("Difficulty must be one of: Beginner, Intermediate, Advanced.");
                parsedDifficulty = parsed;
            }

            var courses = await _courseRepo.SearchAsync(q, category, parsedDifficulty, sort);
            var result = courses.Select(c => c.ToDto());
            return Ok(result);
        }

        // ✅ GET: api/courses/{id}
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            return Ok(course.ToDto());
        }

        // ✅ POST: api/courses
        // Create course + optional thumbnail upload (multipart/form-data)
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Create([FromForm] CourseCreateFormDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title is required.");

            // ✅ Safe parsing for Difficulty
            if (!Enum.TryParse<DifficultyLevel>(dto.Difficulty, ignoreCase: true, out var difficulty))
                return BadRequest("Difficulty must be one of: Beginner, Intermediate, Advanced.");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized("Invalid token (missing NameIdentifier).");

            string? thumbnailUrl = null;
            string? thumbnailPublicId = null;

            if (dto.ThumbnailFile != null && dto.ThumbnailFile.Length > 0)
            {
                var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
                if (!allowed.Contains(dto.ThumbnailFile.ContentType))
                    return BadRequest("Thumbnail must be JPG/PNG/WEBP.");

                // Cloudinary upload
                (thumbnailUrl, thumbnailPublicId) =
                    await _cloud.UploadImageAsync(dto.ThumbnailFile, "olp/courses");
            }

            var course = new Course
            {
                Title = dto.Title.Trim(),
                ShortDescription = dto.ShortDescription?.Trim() ?? "",
                LongDescription = dto.LongDescription?.Trim() ?? "",
                Category = dto.Category?.Trim() ?? "",
                Difficulty = difficulty,
                EstimatedDuration = dto.EstimatedDuration,

                ThumbnailUrl = thumbnailUrl,
                ThumbnailPublicId = thumbnailPublicId,

                CreatedById = userId,
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };

            await _courseService.CreateCourseAsync(course);

            // IMPORTANT:
            // After create, course.Creator may be null unless you explicitly load it.
            // We return DTO without forcing navigation load.
            return Ok(course.ToDto());
        }

        // ✅ PUT: api/courses/{id}
        // Update course + optional thumbnail replacement
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(CourseResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Update(int id, [FromForm] CourseCreateFormDto dto)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            if (string.IsNullOrWhiteSpace(dto.Title))
                return BadRequest("Title is required.");

            if (!Enum.TryParse<DifficultyLevel>(dto.Difficulty, ignoreCase: true, out var difficulty))
                return BadRequest("Difficulty must be one of: Beginner, Intermediate, Advanced.");

            course.Title = dto.Title.Trim();
            course.ShortDescription = dto.ShortDescription?.Trim() ?? "";
            course.LongDescription = dto.LongDescription?.Trim() ?? "";
            course.Category = dto.Category?.Trim() ?? "";
            course.Difficulty = difficulty;
            course.EstimatedDuration = dto.EstimatedDuration;

            if (dto.ThumbnailFile != null && dto.ThumbnailFile.Length > 0)
            {
                var allowed = new[] { "image/jpeg", "image/png", "image/webp" };
                if (!allowed.Contains(dto.ThumbnailFile.ContentType))
                    return BadRequest("Thumbnail must be JPG/PNG/WEBP.");

                var (url, publicId) = await _cloud.UploadImageAsync(dto.ThumbnailFile, "olp/courses");
                course.ThumbnailUrl = url;
                course.ThumbnailPublicId = publicId;
            }

            await _courseRepo.SaveChangesAsync();

            return Ok(course.ToDto());
        }

        // ✅ POST: api/courses/{id}/publish
        [HttpPost("{id:int}/publish")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Publish(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            course.IsPublished = true;
            await _courseRepo.SaveChangesAsync();

            return Ok(new { message = "Course published" });
        }

        // ? POST: api/courses/{id}/unpublish
        [HttpPost("{id:int}/unpublish")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Unpublish(int id)
        {
            var course = await _courseRepo.GetByIdAsync(id);
            if (course == null) return NotFound();

            course.IsPublished = false;
            await _courseRepo.SaveChangesAsync();

            return Ok(new { message = "Course unpublished" });
        }
    }
}
