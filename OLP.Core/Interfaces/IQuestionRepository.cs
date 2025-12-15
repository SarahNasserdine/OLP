using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
	public interface IQuestionRepository
	{
		Task<IEnumerable<Question>> GetByQuizIdAsync(int quizId);
		Task AddAsync(Question question);
		Task DeleteAsync(Question question);
		Task SaveChangesAsync();
        Task<Question?> GetByIdAsync(int id);

    }
}
