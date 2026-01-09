using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using OLP.Core.Interfaces;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase
    {
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly ILessonRepository _lessonRepo;
        private readonly IQuizRepository _quizRepo;
        private readonly IQuizAttemptRepository _attemptRepo;
        private readonly ICourseRepository _courseRepo;

        public DashboardController(
            IEnrollmentRepository enrollmentRepo,
            ILessonRepository lessonRepo,
            IQuizRepository quizRepo,
            IQuizAttemptRepository attemptRepo,
            ICourseRepository courseRepo)
        {
            _enrollmentRepo = enrollmentRepo;
            _lessonRepo = lessonRepo;
            _quizRepo = quizRepo;
            _attemptRepo = attemptRepo;
            _courseRepo = courseRepo;
        }

        // GET /api/dashboard/student
        [HttpGet("student")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentDashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var enrollments = (await _enrollmentRepo.GetByUserWithCourseAsync(userId)).ToList();

            var lastAccessedLessons = new Dictionary<int, string?>();
            foreach (var enrollment in enrollments)
            {
                if (enrollment.LastAccessedLessonId.HasValue)
                {
                    var lesson = await _lessonRepo.GetByIdAsync(enrollment.LastAccessedLessonId.Value);
                    lastAccessedLessons[enrollment.Id] = lesson?.Title;
                }
            }

            var courseIds = enrollments.Select(e => e.CourseId).Distinct().ToList();
            var quizzes = (await _quizRepo.GetByCourseIdsAsync(courseIds)).ToList();
            var attemptedIds = await _attemptRepo.GetAttemptedQuizIdsAsync(userId);

            var upcomingQuizzes = quizzes
                .Where(q => !attemptedIds.Contains(q.Id))
                .Take(5)
                .Select(q => new
                {
                    q.Id,
                    q.Title,
                    q.CourseId
                });

            var recentAttempts = (await _attemptRepo.GetByUserRecentAsync(userId, 10)).ToList();
            var avgScore = recentAttempts.Any() ? recentAttempts.Average(a => a.Score) : 0;

            var response = new
            {
                enrollments = enrollments.Select(e => new
                {
                    e.Id,
                    e.CourseId,
                    CourseTitle = e.Course?.Title ?? "",
                    e.Progress,
                    e.LastAccessedLessonId,
                    e.LastAccessedAt,
                    LastAccessedLessonTitle = lastAccessedLessons.GetValueOrDefault(e.Id)
                }),
                upcomingQuizzes,
                scoresSummary = new
                {
                    averageScore = avgScore,
                    recentAttempts = recentAttempts.Select(a => new { a.Id, a.QuizId, a.Score, a.AttemptDate })
                }
            };

            return Ok(response);
        }

        // GET /api/dashboard/admin
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

            var coursesCreated = await _courseRepo.CountByCreatorAsync(userId);
            var totalEnrollments = await _enrollmentRepo.CountAllAsync();
            var overallAverage = await _attemptRepo.GetOverallAverageScoreAsync();
            var quizAverages = (await _attemptRepo.GetQuizAveragesAsync()).ToList();

            var response = new
            {
                coursesCreated,
                totalEnrollments,
                overallAverageScore = overallAverage,
                topQuizzes = quizAverages.OrderByDescending(q => q.AvgScore).Take(3),
                lowQuizzes = quizAverages.OrderBy(q => q.AvgScore).Take(3)
            };

            return Ok(response);
        }
    }
}
