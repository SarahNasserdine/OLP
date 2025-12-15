using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;

namespace OLP.Infrastructure.Repositories
{
    public class QuizAttemptAnswerRepository : IQuizAttemptAnswerRepository
    {
        private readonly AppDbContext _context;
        public QuizAttemptAnswerRepository(AppDbContext context) => _context = context;

        public async Task AddAsync(QuizAttemptAnswer entity) =>
            await _context.QuizAttemptAnswers.AddAsync(entity);

        public async Task<IEnumerable<QuizAttemptAnswer>> GetByAttemptIdAsync(int attemptId) =>
            await _context.QuizAttemptAnswers
                .Where(a => a.QuizAttemptId == attemptId)
                .Include(a => a.Question)
                .Include(a => a.Answer)
                .ToListAsync();

        public async Task SaveChangesAsync() => await _context.SaveChangesAsync();
    }
}
