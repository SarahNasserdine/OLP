using Microsoft.EntityFrameworkCore;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Core.DTOs;
using OLP.Infrastructure.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace OLP.Infrastructure.Repositories
{
    public class QuizAttemptRepository : IQuizAttemptRepository
    {
        private readonly AppDbContext _context;

        public QuizAttemptRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(QuizAttempt attempt)
        {
            await _context.QuizAttempts.AddAsync(attempt);
        }

        public async Task<QuizAttempt?> GetByIdAsync(int id)
        {
            return await _context.QuizAttempts.FindAsync(id);
        }

        public async Task<QuizAttempt?> GetWithAnswersAsync(int id)
        {
            return await _context.QuizAttempts
                .Include(a => a.Answers)
                .ThenInclude(ans => ans.Question)
                .Include(a => a.Answers)
                .ThenInclude(ans => ans.Answer)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<IEnumerable<QuizAttempt>> GetByUserIdAsync(int userId)
        {
            return await _context.QuizAttempts
                .Where(a => a.UserId == userId)
                .Include(a => a.Quiz)
                .Include(a => a.Answers)
                .ToListAsync();
        }

        public async Task<IEnumerable<QuizAttempt>> GetByUserRecentAsync(int userId, int take = 10)
        {
            return await _context.QuizAttempts
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AttemptDate)
                .Take(take)
                .ToListAsync();
        }

        public async Task<double> GetOverallAverageScoreAsync()
        {
            return await _context.QuizAttempts.AnyAsync()
                ? await _context.QuizAttempts.AverageAsync(a => a.Score)
                : 0;
        }

        public async Task<IEnumerable<QuizAvgDto>> GetQuizAveragesAsync()
        {
            return await _context.QuizAttempts
                .Include(a => a.Quiz)
                .GroupBy(a => new { a.QuizId, a.Quiz.Title })
                .Select(g => new QuizAvgDto
                {
                    QuizId = g.Key.QuizId,
                    Title = g.Key.Title,
                    AvgScore = g.Average(x => x.Score),
                    AttemptsCount = g.Count()
                })
                .OrderByDescending(x => x.AvgScore)
                .ToListAsync();
        }

        public async Task<HashSet<int>> GetAttemptedQuizIdsAsync(int userId)
        {
            var ids = await _context.QuizAttempts
                .Where(a => a.UserId == userId)
                .Select(a => a.QuizId)
                .Distinct()
                .ToListAsync();
            return ids.ToHashSet();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
