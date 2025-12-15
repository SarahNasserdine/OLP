using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface IQuizAttemptAnswerRepository
    {
        Task AddAsync(QuizAttemptAnswer entity);
        Task<IEnumerable<QuizAttemptAnswer>> GetByAttemptIdAsync(int attemptId);
        Task SaveChangesAsync();
    }
}
