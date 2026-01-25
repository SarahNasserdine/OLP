using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using OLP.Api.DTOs.Lessons;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Api.Services;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonRepository _lessonRepo;
        private readonly ILessonCompletionRepository _completionRepo;
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly ICloudinaryService _cloud;

        public LessonsController(
            ILessonRepository lessonRepo,
            ILessonCompletionRepository completionRepo,
            IEnrollmentRepository enrollmentRepo,
            ICloudinaryService cloud)
        {
            _lessonRepo = lessonRepo;
            _completionRepo = completionRepo;
            _enrollmentRepo = enrollmentRepo;
            _cloud = cloud;
        }

        // GET /api/courses/{courseId}/lessons
        [HttpGet("courses/{courseId}/lessons")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLessons(int courseId)
        {
            var lessons = await _lessonRepo.GetByCourseIdAsync(courseId);

            var result = lessons.Select(l => new LessonResponseDto
            {
                Id = l.Id,
                CourseId = l.CourseId,
                Title = l.Title,
                Content = l.Content,
                VideoUrl = l.VideoUrl,
                VideoPublicId = l.VideoPublicId,
                AttachmentUrl = l.AttachmentUrl,
                AttachmentPublicId = l.AttachmentPublicId,
                LessonType = l.LessonType,
                OrderIndex = l.OrderIndex,
                EstimatedDuration = l.EstimatedDuration,
                CreatedAt = l.CreatedAt
            });

            return Ok(result);
        }

        // GET /api/courses/{courseId}/lessons/completed
        [HttpGet("courses/{courseId}/lessons/completed")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetCompletedLessons(int courseId)
        {
            if (courseId <= 0)
                return BadRequest("CourseId must be a positive number.");

            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var completions = await _completionRepo.GetByUserAsync(userId);

            var lessonIds = completions
                .Where(c => c.Lesson != null && c.Lesson.CourseId == courseId)
                .Select(c => c.LessonId)
                .Distinct()
                .ToList();

            return Ok(lessonIds);
        }

        // POST /api/courses/{courseId}/lessons
        [HttpPost("courses/{courseId}/lessons")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateLesson(int courseId, [FromForm] LessonCreateFormDto dto)
        {
            var lessonType = (dto.LessonType ?? "Text").Trim();

            var isText = lessonType.Equals("Text", StringComparison.OrdinalIgnoreCase);
            var isVideo = lessonType.Equals("Video", StringComparison.OrdinalIgnoreCase);
            var isPdf = lessonType.Equals("Pdf", StringComparison.OrdinalIgnoreCase)
                     || lessonType.Equals("File", StringComparison.OrdinalIgnoreCase);

            if (!isText && !isVideo && !isPdf)
                return BadRequest("LessonType must be Text, Video, or Pdf.");

            string? videoUrl = string.IsNullOrWhiteSpace(dto.VideoUrl) ? null : dto.VideoUrl.Trim();
            string? videoPublicId = null;

            string? attachmentUrl = string.IsNullOrWhiteSpace(dto.AttachmentUrl) ? null : dto.AttachmentUrl.Trim();
            string? attachmentPublicId = null;

            if (isVideo)
            {
                if (dto.VideoFile != null && dto.VideoFile.Length > 0)
                {
                    if (!dto.VideoFile.ContentType.StartsWith("video/"))
                        return BadRequest("Uploaded VideoFile must be a video.");

                    (videoUrl, videoPublicId) =
                        await _cloud.UploadVideoAsync(dto.VideoFile, "olp/lessons/videos");
                }

                if (string.IsNullOrWhiteSpace(videoUrl))
                    return BadRequest("Provide VideoFile or VideoUrl when LessonType is Video.");
            }

            if (isPdf)
            {
                if (dto.AttachmentFile != null && dto.AttachmentFile.Length > 0)
                {
                    if (dto.AttachmentFile.ContentType != "application/pdf")
                        return BadRequest("Uploaded AttachmentFile must be a PDF.");

                    (attachmentUrl, attachmentPublicId) =
                        await _cloud.UploadFileAsync(dto.AttachmentFile, "olp/lessons/pdfs");
                }

                if (string.IsNullOrWhiteSpace(attachmentUrl))
                    return BadRequest("Provide AttachmentFile or AttachmentUrl when LessonType is Pdf/File.");
            }

            if (isText && string.IsNullOrWhiteSpace(dto.Content))
                return BadRequest("Content is required for Text lesson.");

            var lesson = new Lesson
            {
                CourseId = courseId,
                Title = dto.Title.Trim(),
                LessonType = isPdf ? "Pdf" : isVideo ? "Video" : "Text",
                Content = isText ? dto.Content : null,
                VideoUrl = videoUrl,
                VideoPublicId = videoPublicId,
                AttachmentUrl = attachmentUrl,
                AttachmentPublicId = attachmentPublicId,
                OrderIndex = dto.OrderIndex,
                EstimatedDuration = dto.EstimatedDuration
            };

            await _lessonRepo.AddAsync(lesson);
            await _lessonRepo.SaveChangesAsync();

            return Ok(new LessonResponseDto
            {
                Id = lesson.Id,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                Content = lesson.Content,
                VideoUrl = lesson.VideoUrl,
                VideoPublicId = lesson.VideoPublicId,
                AttachmentUrl = lesson.AttachmentUrl,
                AttachmentPublicId = lesson.AttachmentPublicId,
                LessonType = lesson.LessonType,
                OrderIndex = lesson.OrderIndex,
                EstimatedDuration = lesson.EstimatedDuration,
                CreatedAt = lesson.CreatedAt
            });
        }

        // PUT /api/lessons/{id}
        [HttpPut("lessons/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateLesson(int id, [FromForm] LessonCreateFormDto dto)
        {
            var lesson = await _lessonRepo.GetByIdAsync(id);
            if (lesson == null) return NotFound("Lesson not found.");

            var lessonType = (dto.LessonType ?? lesson.LessonType ?? "Text").Trim();

            var isText = lessonType.Equals("Text", StringComparison.OrdinalIgnoreCase);
            var isVideo = lessonType.Equals("Video", StringComparison.OrdinalIgnoreCase);
            var isPdf = lessonType.Equals("Pdf", StringComparison.OrdinalIgnoreCase)
                     || lessonType.Equals("File", StringComparison.OrdinalIgnoreCase);

            if (!isText && !isVideo && !isPdf)
                return BadRequest("LessonType must be Text, Video, or Pdf.");

            string? videoUrl = string.IsNullOrWhiteSpace(dto.VideoUrl) ? lesson.VideoUrl : dto.VideoUrl.Trim();
            string? attachmentUrl = string.IsNullOrWhiteSpace(dto.AttachmentUrl) ? lesson.AttachmentUrl : dto.AttachmentUrl.Trim();

            if (isVideo && dto.VideoFile != null && dto.VideoFile.Length > 0)
            {
                if (!dto.VideoFile.ContentType.StartsWith("video/"))
                    return BadRequest("Uploaded VideoFile must be a video.");

                (videoUrl, lesson.VideoPublicId) =
                    await _cloud.UploadVideoAsync(dto.VideoFile, "olp/lessons/videos");
            }

            if (isPdf && dto.AttachmentFile != null && dto.AttachmentFile.Length > 0)
            {
                if (dto.AttachmentFile.ContentType != "application/pdf")
                    return BadRequest("Uploaded AttachmentFile must be a PDF.");

                (attachmentUrl, lesson.AttachmentPublicId) =
                    await _cloud.UploadFileAsync(dto.AttachmentFile, "olp/lessons/pdfs");
            }

            lesson.Title = dto.Title.Trim();
            lesson.LessonType = isPdf ? "Pdf" : isVideo ? "Video" : "Text";
            lesson.Content = isText ? dto.Content : null;
            lesson.VideoUrl = isVideo ? videoUrl : null;
            lesson.AttachmentUrl = isPdf ? attachmentUrl : null;
            lesson.OrderIndex = dto.OrderIndex;
            lesson.EstimatedDuration = dto.EstimatedDuration;

            await _lessonRepo.UpdateAsync(lesson);
            await _lessonRepo.SaveChangesAsync();

            return Ok(new LessonResponseDto
            {
                Id = lesson.Id,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                Content = lesson.Content,
                VideoUrl = lesson.VideoUrl,
                VideoPublicId = lesson.VideoPublicId,
                AttachmentUrl = lesson.AttachmentUrl,
                AttachmentPublicId = lesson.AttachmentPublicId,
                LessonType = lesson.LessonType,
                OrderIndex = lesson.OrderIndex,
                EstimatedDuration = lesson.EstimatedDuration,
                CreatedAt = lesson.CreatedAt
            });
        }

        // DELETE /api/lessons/{id}
        [HttpDelete("lessons/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteLesson(int id)
        {
            var lesson = await _lessonRepo.GetByIdAsync(id);
            if (lesson == null) return NotFound("Lesson not found.");

            if (!string.IsNullOrWhiteSpace(lesson.VideoPublicId))
                await _cloud.DeleteAsync(lesson.VideoPublicId, "video");

            if (!string.IsNullOrWhiteSpace(lesson.AttachmentPublicId))
                await _cloud.DeleteAsync(lesson.AttachmentPublicId, "raw");

            await _lessonRepo.DeleteAsync(lesson);
            await _lessonRepo.SaveChangesAsync();

            return Ok(new { message = "Lesson deleted successfully." });
        }

        // POST /api/lessons/{id}/access
        [HttpPost("lessons/{id}/access")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> AccessLesson(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var lesson = await _lessonRepo.GetByIdAsync(id);
            if (lesson == null) return NotFound("Lesson not found.");

            var enrollment = await _enrollmentRepo.GetByUserAndCourseAsync(userId, lesson.CourseId);
            if (enrollment == null) return BadRequest("Enrollment not found.");

            enrollment.LastAccessedLessonId = id;
            enrollment.LastAccessedAt = DateTime.UtcNow;
            await _enrollmentRepo.SaveChangesAsync();

            return Ok(new { message = "Lesson accessed." });
        }

        // POST /api/lessons/{id}/complete
        [HttpPost("lessons/{id}/complete")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CompleteLesson(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var lesson = await _lessonRepo.GetByIdAsync(id);
            if (lesson == null) return NotFound("Lesson not found.");

            if (await _completionRepo.IsCompletedAsync(userId, id))
                return BadRequest("Lesson already completed.");

            var completion = new LessonCompletion
            {
                UserId = userId,
                LessonId = id
            };

            await _completionRepo.AddAsync(completion);
            await _completionRepo.SaveChangesAsync();

            var totalLessons = await _lessonRepo.CountByCourseAsync(lesson.CourseId);
            var completedCount = await _completionRepo.CountCompletedInCourseAsync(userId, lesson.CourseId);
            var enrollment = await _enrollmentRepo.GetByUserAndCourseAsync(userId, lesson.CourseId);
            if (enrollment != null && totalLessons > 0)
            {
                enrollment.Progress = Math.Round((decimal)completedCount / totalLessons * 100, 2);
                enrollment.LastAccessedLessonId = id;
                enrollment.LastAccessedAt = DateTime.UtcNow;
                await _enrollmentRepo.SaveChangesAsync();
            }

            return Ok(new { message = "Lesson marked as completed." });
        }
    }
}
