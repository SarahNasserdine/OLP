using OLP.Core.Entities;
using OLP.Core.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OLP.Core.Interfaces
{
    public interface IQuizAttemptRepository
    {
        Task AddAsync(QuizAttempt attempt);
        Task<QuizAttempt?> GetByIdAsync(int id);
        Task<QuizAttempt?> GetWithAnswersAsync(int id);
        Task<IEnumerable<QuizAttempt>> GetByUserIdAsync(int userId);
        Task<IEnumerable<QuizAttempt>> GetByUserRecentAsync(int userId, int take = 10);
        Task<double> GetOverallAverageScoreAsync();
        Task<IEnumerable<QuizAvgDto>> GetQuizAveragesAsync();
        Task<HashSet<int>> GetAttemptedQuizIdsAsync(int userId);
        Task SaveChangesAsync();
    }
}
