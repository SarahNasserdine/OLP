using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface IAnswerRepository
    {
        Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId);
        Task AddAsync(Answer answer);
        Task DeleteAsync(Answer answer);
        Task SaveChangesAsync();
        Task<Answer?> GetByIdAsync(int id);

    }
}
