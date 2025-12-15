using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
    public interface IQuizRepository
    {
        Task<Quiz?> GetByIdAsync(int id);
        Task<Quiz?> GetByIdWithQuestionsAsync(int id);
        Task AddAsync(Quiz quiz);
        Task<IEnumerable<Quiz>> GetByCourseIdAsync(int courseId);
        Task SaveChangesAsync();
    }
}
