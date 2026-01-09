using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Api.DTOs.Enrollments;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class EnrollmentsController : ControllerBase
    {
        private readonly IEnrollmentRepository _enrollRepo;

        public EnrollmentsController(IEnrollmentRepository enrollRepo)
        {
            _enrollRepo = enrollRepo;
        }

        // POST /api/courses/{courseId}/enroll
        [HttpPost("courses/{courseId}/enroll")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr))
                return Unauthorized("Missing user id in token.");

            var userId = int.Parse(userIdStr);

            var existing = await _enrollRepo.GetByUserAndCourseAsync(userId, courseId);
            if (existing != null)
                return BadRequest("Already enrolled.");

            var enroll = new Enrollment
            {
                UserId = userId,
                CourseId = courseId,
                EnrollDate = DateTime.UtcNow,
                Progress = 0
            };

            await _enrollRepo.AddAsync(enroll);
            await _enrollRepo.SaveChangesAsync();

            var response = new EnrollResponseDto
            {
                EnrollmentId = enroll.Id,
                CourseId = enroll.CourseId,
                UserId = enroll.UserId,
                EnrollDate = enroll.EnrollDate,
                Message = "Enrolled successfully."
            };

            return Ok(response);
        }

        // GET /api/my/enrollments
        [HttpGet("my/enrollments")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyEnrollments()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr))
                return Unauthorized("Missing user id in token.");

            var userId = int.Parse(userIdStr);

            // IMPORTANT: Repo should Include Course, but not Course.Enrollments
            var enrolls = await _enrollRepo.GetByUserAsync(userId);

            var result = enrolls.Select(e => new EnrollmentResponseDto
            {
                Id = e.Id,
                CourseId = e.CourseId,
                CourseTitle = e.Course?.Title ?? "",
                CourseThumbnailUrl = e.Course?.ThumbnailUrl,
                UserId = e.UserId,
                UserFullName = e.User?.FullName ?? "",
                UserEmail = e.User?.Email ?? "",
                EnrollDate = e.EnrollDate,
                Progress = e.Progress,
                LastAccessedLessonId = e.LastAccessedLessonId,
                LastAccessedAt = e.LastAccessedAt
            });

            return Ok(result);
        }

        // GET /api/courses/{courseId}/enrollments (Admin/SuperAdmin)
        [HttpGet("courses/{courseId}/enrollments")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CourseEnrollments(int courseId)
        {
            var enrolls = await _enrollRepo.GetByCourseAsync(courseId);

            var result = enrolls.Select(e => new EnrollmentResponseDto
            {
                Id = e.Id,
                CourseId = e.CourseId,
                CourseTitle = e.Course?.Title ?? "",
                CourseThumbnailUrl = e.Course?.ThumbnailUrl,
                UserId = e.UserId,
                UserFullName = e.User?.FullName ?? "",
                UserEmail = e.User?.Email ?? "",
                EnrollDate = e.EnrollDate,
                Progress = e.Progress,
                LastAccessedLessonId = e.LastAccessedLessonId,
                LastAccessedAt = e.LastAccessedAt
            });

            return Ok(new
            {
                courseId,
                totalEnrollments = result.Count(),
                enrollments = result
            });
        }
    }
}
