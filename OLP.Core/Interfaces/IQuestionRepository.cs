using OLP.Core.Entities;

namespace OLP.Core.Interfaces
{
	public interface IQuestionRepository
	{
		Task<IEnumerable<Question>> GetByQuizIdAsync(int quizId);
        Task<IEnumerable<Question>> GetByIdsWithAnswersAsync(IEnumerable<int> questionIds);
        Task<IEnumerable<Question>> GetLessonQuestionsByCourseIdAsync(int courseId);
		Task AddAsync(Question question);
		Task DeleteAsync(Question question);
		Task SaveChangesAsync();
        Task<Question?> GetByIdAsync(int id);

    }
}
