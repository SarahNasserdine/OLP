using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using OLP.Core.Interfaces;
using OLP.Core.Entities;
using OLP.Api.Services;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api")]
    public class CertificatesController : ControllerBase
    {
        private readonly ICertificateRepository _certRepo;
        private readonly IUserRepository _userRepo;
        private readonly ICourseRepository _courseRepo;
        private readonly ILessonRepository _lessonRepo;
        private readonly ILessonCompletionRepository _completionRepo;
        private readonly IQuizRepository _quizRepo;
        private readonly IQuizAttemptRepository _attemptRepo;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly CertificatePdfBuilder _pdfBuilder;

        public CertificatesController(
            ICertificateRepository certRepo,
            IUserRepository userRepo,
            ICourseRepository courseRepo,
            ILessonRepository lessonRepo,
            ILessonCompletionRepository completionRepo,
            IQuizRepository quizRepo,
            IQuizAttemptRepository attemptRepo,
            ICloudinaryService cloudinaryService,
            CertificatePdfBuilder pdfBuilder)
        {
            _certRepo = certRepo;
            _userRepo = userRepo;
            _courseRepo = courseRepo;
            _lessonRepo = lessonRepo;
            _completionRepo = completionRepo;
            _quizRepo = quizRepo;
            _attemptRepo = attemptRepo;
            _cloudinaryService = cloudinaryService;
            _pdfBuilder = pdfBuilder;
        }

        [HttpPost("courses/{courseId}/certificate/generate")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GenerateForStudent(int courseId, [FromQuery] int userId)
        {
            var cert = await GenerateCertificateAsync(userId, courseId);
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

            var quizzes = (await _quizRepo.GetByCourseIdAsync(courseId))
                .Where(q => q.IsActive)
                .ToList();
            var attempts = await _attemptRepo.GetByUserIdAsync(userId);

            var finalQuizzes = quizzes.Where(q => q.IsFinal).ToList();

            var requiredQuizzes = quizzes.Where(q => !q.IsFinal).ToList();
            var missing = requiredQuizzes
                .Select(q => new
                {
                    Quiz = q,
                    BestScore = attempts
                        .Where(a => a.QuizId == q.Id)
                        .Select(a => a.Score)
                        .DefaultIfEmpty(-1)
                        .Max()
                })
                .Where(x => x.BestScore < x.Quiz.PassingScore)
                .ToList();

            if (finalQuizzes.Any())
            {
                var finalAttempts = attempts
                    .Where(a => finalQuizzes.Any(q => q.Id == a.QuizId))
                    .ToList();

                if (!finalAttempts.Any())
                {
                    missing.Add(new
                    {
                        Quiz = finalQuizzes.OrderByDescending(q => q.Id).First(),
                        BestScore = -1
                    });
                }
                else
                {
                    var bestAttempt = finalAttempts.OrderByDescending(a => a.Score).First();
                    var bestFinalQuiz = finalQuizzes.First(q => q.Id == bestAttempt.QuizId);
                    if (bestAttempt.Score < bestFinalQuiz.PassingScore)
                    {
                        missing.Add(new
                        {
                            Quiz = bestFinalQuiz,
                            BestScore = bestAttempt.Score
                        });
                    }
                }
            }

            if (missing.Any())
            {
                var details = string.Join(
                    "; ",
                    missing.Select(x =>
                        $"{x.Quiz.Title} (score {Math.Max(0, x.BestScore)}%, pass {x.Quiz.PassingScore}%)"));
                return BadRequest($"Complete all required quizzes before generating a certificate. Missing: {details}");
            }

            var cert = await GenerateCertificateAsync(userId, courseId);
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

            var result = certs.Select(c => new
            {
                c.Id,
                c.CourseId,
                CourseTitle = c.Course?.Title ?? "",
                c.DownloadUrl,
                c.VerificationCode,
                c.Status,
                c.GeneratedAt
            });

            return Ok(result);
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

        private async Task<Certificate> GenerateCertificateAsync(int userId, int courseId)
        {
            var existing = await _certRepo.GetByUserAndCourseAsync(userId, courseId);
            if (existing != null && IsValidCertificateUrl(existing.DownloadUrl))
                return existing;

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found.");

            var course = await _courseRepo.GetByIdAsync(courseId);
            if (course == null)
                throw new Exception("Course not found.");

            var pdfBytes = await _pdfBuilder.BuildAsync(user.FullName, course.Title, DateTime.UtcNow);

            using var pdfStream = new MemoryStream(pdfBytes);
            var formFile = new FormFile(pdfStream, 0, pdfBytes.Length, "certificate", $"certificate-{userId}-{courseId}.pdf")
            {
                Headers = new HeaderDictionary(),
                ContentType = "application/pdf"
            };

            var (url, publicId) = await _cloudinaryService.UploadFileAsync(formFile, "olp/certificates");

            var verificationCode = Guid.NewGuid().ToString("N").Substring(0, 10);

            if (existing != null)
            {
                existing.DownloadUrl = url;
                existing.VerificationCode = verificationCode;
                existing.Status = "Generated";
                existing.GeneratedAt = DateTime.UtcNow;
                await _certRepo.SaveChangesAsync();
                return existing;
            }

            var certificate = new Certificate
            {
                UserId = userId,
                CourseId = courseId,
                DownloadUrl = url,
                VerificationCode = verificationCode,
                Status = "Generated",
                GeneratedAt = DateTime.UtcNow
            };

            await _certRepo.AddAsync(certificate);
            await _certRepo.SaveChangesAsync();

            return certificate;
        }

        private static bool IsValidCertificateUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return false;

            return url.Contains("res.cloudinary.com", StringComparison.OrdinalIgnoreCase)
                   && url.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);
        }
    }
}
