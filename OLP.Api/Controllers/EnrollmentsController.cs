using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OLP.Core.Entities;
using OLP.Core.Interfaces;

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

        [HttpPost("courses/{courseId}/enroll")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> Enroll(int courseId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var existing = await _enrollRepo.GetByUserAndCourseAsync(userId, courseId);
            if (existing != null) return BadRequest("Already enrolled");

            var enroll = new Enrollment
            {
                UserId = userId,
                CourseId = courseId
            };

            await _enrollRepo.AddAsync(enroll);
            await _enrollRepo.SaveChangesAsync();

            return Ok(enroll);
        }

        [HttpGet("my/enrollments")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> MyEnrollments()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var enrolls = await _enrollRepo.GetByUserAsync(userId);
            return Ok(enrolls);
        }

        [HttpGet("courses/{courseId}/enrollments")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CourseEnrollments(int courseId)
        {
            // simple: filter client-side from all enrollments
            // or create a repo method by course if you want
            return Ok("Add course-based analytics here if needed.");
        }
    }
}

// POST /api/courses/{courseId}/enroll → Student

// GET / api / my / enrollments → Student

// GET /api/courses/{courseId}/ enrollments → Admin / SuperAdmin(analytics)