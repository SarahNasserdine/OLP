using OLP.Core.Entities;
using OLP.Core.Interfaces;

namespace OLP.Infrastructure.Services
{
    public class CourseService
    {
        private readonly ICourseRepository _courseRepo;
        private readonly IEnrollmentRepository _enrollRepo;
        private readonly ILessonCompletionRepository _completionRepo;

        public CourseService(ICourseRepository courseRepo, IEnrollmentRepository enrollRepo, ILessonCompletionRepository completionRepo)
        {
            _courseRepo = courseRepo;
            _enrollRepo = enrollRepo;
            _completionRepo = completionRepo;
        }

        public async Task<Course> CreateCourseAsync(Course course)
        {
            await _courseRepo.AddAsync(course);
            await _courseRepo.SaveChangesAsync();
            return course;
        }

        public async Task EnrollAsync(int userId, int courseId)
        {
            var exists = await _enrollRepo.GetByUserAndCourseAsync(userId, courseId);
            if (exists != null)
                throw new Exception("User already enrolled in this course");

            var enrollment = new Enrollment
            {
                UserId = userId,
                CourseId = courseId
            };

            await _enrollRepo.AddAsync(enrollment);
            await _enrollRepo.SaveChangesAsync();
        }

        public async Task<decimal> CalculateProgressAsync(int userId, int courseId)
        {
            var lessons = await _courseRepo.GetByIdAsync(courseId);
            if (lessons == null) return 0;

            var totalLessons = lessons.Lessons?.Count ?? 0;
            if (totalLessons == 0) return 0;

            var completed = (await _completionRepo.GetByUserAsync(userId))
                .Count(lc => lc.Lesson.CourseId == courseId);

            return (decimal)completed / totalLessons * 100;
        }
    }
}
