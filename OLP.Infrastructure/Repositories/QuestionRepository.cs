using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace OLP.Infrastructure.Repositories
{
    public class QuestionRepository : IQuestionRepository
    {
        private readonly AppDbContext _context;

        public QuestionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Question>> GetByQuizIdAsync(int quizId)
        {
            return await _context.Questions
                .Where(q => q.QuizId == quizId)
                .Include(q => q.Answers)
                .ToListAsync();
        }

        public async Task<IEnumerable<Question>> GetByIdsWithAnswersAsync(IEnumerable<int> questionIds)
        {
            var ids = questionIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
                return new List<Question>();

            return await _context.Questions
                .Where(q => ids.Contains(q.Id))
                .Include(q => q.Answers)
                .ToListAsync();
        }

        public async Task<IEnumerable<Question>> GetLessonQuestionsByCourseIdAsync(int courseId)
        {
            return await _context.Questions
                .Include(q => q.Answers)
                .Where(q => q.Quiz.CourseId == courseId && q.Quiz.LessonId != null)
                .ToListAsync();
        }

        // ✅ Add GetByIdAsync method
        public async Task<Question?> GetByIdAsync(int id)
        {
            return await _context.Questions.FindAsync(id);
        }

        public async Task AddAsync(Question question)
        {
            await _context.Questions.AddAsync(question);
        }

        public async Task DeleteAsync(Question question)
        {
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
