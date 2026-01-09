using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Services;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class CertificatesController : ControllerBase
    {
        private readonly CertificateService _certService;
        private readonly ICertificateRepository _certRepo;
        private readonly ILessonRepository _lessonRepo;
        private readonly ILessonCompletionRepository _completionRepo;
        private readonly IQuizRepository _quizRepo;
        private readonly IQuizAttemptRepository _attemptRepo;

        public CertificatesController(
            CertificateService certService,
            ICertificateRepository certRepo,
            ILessonRepository lessonRepo,
            ILessonCompletionRepository completionRepo,
            IQuizRepository quizRepo,
            IQuizAttemptRepository attemptRepo)
        {
            _certService = certService;
            _certRepo = certRepo;
            _lessonRepo = lessonRepo;
            _completionRepo = completionRepo;
            _quizRepo = quizRepo;
            _attemptRepo = attemptRepo;
        }

        [HttpPost("courses/{courseId}/certificate/generate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GenerateForStudent(int courseId, [FromQuery] int userId)
        {
            var cert = await _certService.GenerateCertificateAsync(userId, courseId);
            return Ok(cert);
        }

        [HttpPost("courses/{courseId}/certificate")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GenerateForSelf(int courseId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var lessons = (await _lessonRepo.GetByCourseIdAsync(courseId)).ToList();
            var completedLessons = await _completionRepo.GetByUserAsync(userId);
            var completedIds = completedLessons.Select(c => c.LessonId).ToHashSet();
            var completedCount = lessons.Count(l => completedIds.Contains(l.Id));

            if (lessons.Count > 0 && completedCount < lessons.Count)
                return BadRequest("Complete all lessons before generating a certificate.");

            var quizzes = (await _quizRepo.GetByCourseIdAsync(courseId)).ToList();
            var attempts = await _attemptRepo.GetByUserIdAsync(userId);

            foreach (var quiz in quizzes)
            {
                var passed = attempts.Any(a => a.QuizId == quiz.Id && a.Score >= quiz.PassingScore);
                if (!passed)
                    return BadRequest("Complete all required quizzes before generating a certificate.");
            }

            var cert = await _certService.GenerateCertificateAsync(userId, courseId);
            return Ok(cert);
        }

        [HttpGet("courses/{courseId}/certificate")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetForSelf(int courseId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var cert = await _certRepo.GetByUserAndCourseAsync(userId, courseId);
            if (cert == null) return NotFound();
            return Ok(cert);
        }

        [HttpGet("my/certificates")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetMyCertificates()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var certs = await _certRepo.GetByUserAsync(userId);
            return Ok(certs);
        }

        [HttpGet("certificates/{id}/download")]
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var cert = await _certRepo.GetByIdAsync(id);
            if (cert == null) return NotFound();
            return Ok(new { cert.DownloadUrl });
        }

        [HttpGet("certificates/verify/{code}")]
        [AllowAnonymous]
        public async Task<IActionResult> Verify(string code)
        {
            var cert = await _certRepo.GetByCodeAsync(code);
            if (cert == null) return NotFound("Invalid or unknown certificate");
            return Ok(cert);
        }
    }
}
