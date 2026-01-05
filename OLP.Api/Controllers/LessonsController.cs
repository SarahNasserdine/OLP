using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OLP.Api.DTOs;          // ✅ Form DTOs live in OLP.Api now
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
        private readonly ICloudinaryService _cloud;

        public LessonsController(
            ILessonRepository lessonRepo,
            ILessonCompletionRepository completionRepo,
            ICloudinaryService cloud)
        {
            _lessonRepo = lessonRepo;
            _completionRepo = completionRepo;
            _cloud = cloud;
        }

        // GET /api/courses/{courseId}/lessons
        [HttpGet("courses/{courseId}/lessons")]
        [Authorize] // Student/Admin/SuperAdmin
        public async Task<IActionResult> GetLessons(int courseId)
        {
            var lessons = await _lessonRepo.GetByCourseIdAsync(courseId);
            return Ok(lessons);
        }

        // POST /api/courses/{courseId}/lessons  (Admin/SuperAdmin)
        // ✅ Create lesson + upload PDF/Video in the SAME request
        [HttpPost("courses/{courseId}/lessons")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateLesson(int courseId, [FromForm] LessonCreateFormDto dto)
        {
            string? videoUrl = null;
            string? videoPublicId = null;

            string? attachmentUrl = null;
            string? attachmentPublicId = null;

            var lessonType = (dto.LessonType ?? "Text").Trim();

            if (lessonType.Equals("Video", StringComparison.OrdinalIgnoreCase))
            {
                if (dto.File == null || dto.File.Length == 0)
                    return BadRequest("Video file is required when LessonType is Video.");

                if (!dto.File.ContentType.StartsWith("video/"))
                    return BadRequest("Uploaded file must be a video.");

                (videoUrl, videoPublicId) =
                    await _cloud.UploadVideoAsync(dto.File, "olp/lessons/videos");
            }
            else if (lessonType.Equals("Pdf", StringComparison.OrdinalIgnoreCase))
            {
                if (dto.File == null || dto.File.Length == 0)
                    return BadRequest("PDF file is required when LessonType is Pdf.");

                if (dto.File.ContentType != "application/pdf")
                    return BadRequest("Uploaded file must be a PDF.");

                (attachmentUrl, attachmentPublicId) =
                    await _cloud.UploadFileAsync(dto.File, "olp/lessons/pdfs");
            }
            else
            {
                // Text lesson
                if (string.IsNullOrWhiteSpace(dto.Content))
                    return BadRequest("Content is required when LessonType is Text.");
            }

            var lesson = new Lesson
            {
                CourseId = courseId,
                Title = dto.Title,
                LessonType = lessonType,
                Content = dto.Content,

                VideoUrl = videoUrl,
                VideoPublicId = videoPublicId,

                AttachmentUrl = attachmentUrl,
                AttachmentPublicId = attachmentPublicId,

                OrderIndex = dto.OrderIndex,
                EstimatedDuration = dto.EstimatedDuration
            };

            await _lessonRepo.AddAsync(lesson);
            await _lessonRepo.SaveChangesAsync();
            return Ok(lesson);
        }

        // POST /api/lessons/{id}/complete (Student)
        [HttpPost("lessons/{id}/complete")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> CompleteLesson(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            if (await _completionRepo.IsCompletedAsync(userId, id))
                return BadRequest("Lesson already completed.");

            var completion = new LessonCompletion
            {
                UserId = userId,
                LessonId = id
            };

            await _completionRepo.AddAsync(completion);
            await _completionRepo.SaveChangesAsync();

            return Ok(new { message = "Lesson marked as completed" });
        }
    }
}
