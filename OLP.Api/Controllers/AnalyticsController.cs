using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OLP.Core.Interfaces;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly IEnrollmentRepository _enrollmentRepo;
        private readonly IQuizAttemptRepository _attemptRepo;
        private readonly IUserRepository _userRepo;

        public AnalyticsController(
            IEnrollmentRepository enrollmentRepo,
            IQuizAttemptRepository attemptRepo,
            IUserRepository userRepo)
        {
            _enrollmentRepo = enrollmentRepo;
            _attemptRepo = attemptRepo;
            _userRepo = userRepo;
        }

        [HttpGet("overview")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetOverview()
        {
            var totalEnrollments = await _enrollmentRepo.CountAllAsync();
            var activeStudents = await _userRepo.CountActiveStudentsAsync();
            var averageQuizScore = await _attemptRepo.GetOverallAverageScoreAsync();
            var completionRate = await _enrollmentRepo.GetAverageProgressAsync();
            var quizAverages = (await _attemptRepo.GetQuizAveragesAsync()).Take(5);

            var enrollments = await _enrollmentRepo.GetAllAsync();
            var weekly = BuildWeeklyEnrollments(enrollments, 4);

            return Ok(new
            {
                totalEnrollments,
                activeStudents,
                averageQuizScore,
                completionRate,
                topQuizzes = quizAverages.Select(q => new { q.QuizId, q.Title, q.AvgScore }),
                enrollmentsByWeek = weekly.Select(w => new { w.Label, w.Count })
            });
        }

        private static IEnumerable<WeeklyEnrollment> BuildWeeklyEnrollments(IEnumerable<Core.Entities.Enrollment> enrollments, int weeks)
        {
            var today = DateTime.UtcNow.Date;
            var startOfWeek = GetStartOfWeek(today);
            var buckets = new List<WeeklyEnrollment>();

            for (var i = weeks - 1; i >= 0; i--)
            {
                var weekStart = startOfWeek.AddDays(-7 * i);
                var weekEnd = weekStart.AddDays(7);
                var count = enrollments.Count(e => e.EnrollDate >= weekStart && e.EnrollDate < weekEnd);
                buckets.Add(new WeeklyEnrollment
                {
                    Label = weekStart.ToString("MMM d"),
                    Count = count
                });
            }

            return buckets;
        }

        private static DateTime GetStartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-diff);
        }

        private class WeeklyEnrollment
        {
            public string Label { get; set; } = "";
            public int Count { get; set; }
        }
    }
}
