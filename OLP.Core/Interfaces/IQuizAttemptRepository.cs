using OLP.Core.Entities;
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
        Task SaveChangesAsync();
    }
}
