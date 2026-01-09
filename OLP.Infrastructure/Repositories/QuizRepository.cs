using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class QuizRepository : IQuizRepository
    {
        private readonly AppDbContext _context;

        public QuizRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Quiz?> GetByIdAsync(int id)
        {
            return await _context.Quizzes
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<Quiz?> GetByIdWithQuestionsAsync(int id)
        {
            return await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<IEnumerable<Quiz>> GetAllWithCourseAsync()
        {
            return await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .ToListAsync();
        }

        public async Task<IEnumerable<Quiz>> GetByCourseIdAsync(int courseId)
        {
            return await _context.Quizzes
                .Include(q => q.Course)
                .Include(q => q.Questions)
                .Where(q => q.CourseId == courseId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Quiz>> GetByCourseIdsAsync(IEnumerable<int> courseIds)
        {
            return await _context.Quizzes
                .Where(q => courseIds.Contains(q.CourseId))
                .ToListAsync();
        }

        public async Task AddAsync(Quiz quiz)
        {
            await _context.Quizzes.AddAsync(quiz);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
