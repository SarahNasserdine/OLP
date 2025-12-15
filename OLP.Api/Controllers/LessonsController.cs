using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OLP.Core.DTOs;
using OLP.Core.Entities;
using OLP.Core.Interfaces;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class LessonsController : ControllerBase
    {
        private readonly ILessonRepository _lessonRepo;
        private readonly ILessonCompletionRepository _completionRepo;

        public LessonsController(ILessonRepository lessonRepo, ILessonCompletionRepository completionRepo)
        {
            _lessonRepo = lessonRepo;
            _completionRepo = completionRepo;
        }

        [HttpGet("courses/{courseId}/lessons")]
        [Authorize]
        public async Task<IActionResult> GetLessons(int courseId)
        {
            var lessons = await _lessonRepo.GetByCourseIdAsync(courseId);
            return Ok(lessons);
        }

        [HttpPost("courses/{courseId}/lessons")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CreateLesson(int courseId, [FromBody] LessonCreateDto dto)
        {
            var lesson = new Lesson
            {
                CourseId = courseId,
                Title = dto.Title,
                Content = dto.Content,
                VideoUrl = dto.VideoUrl,
                AttachmentUrl = dto.AttachmentUrl,
                OrderIndex = dto.OrderIndex,
                EstimatedDuration = dto.EstimatedDuration
            };

            await _lessonRepo.AddAsync(lesson);
            await _lessonRepo.SaveChangesAsync();
            return Ok(lesson);
        }

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
// GET /api/courses/{courseId}/lessons → Student/Admin/SuperAdmin

//POST / api / courses /{ courseId}/ lessons → Admin / SuperAdmin(add lesson)

//POST / api / lessons /{ id}/ complete → Student