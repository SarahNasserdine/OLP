using System.Threading.Tasks;
using OLP.Core.Entities;
using OLP.Core.DTOs;

namespace OLP.Core.Interfaces

{
    public interface IQuizService
    {
        Task<QuizAttempt> StartAttemptAsync(int userId, int quizId);
        Task<QuizAttempt> StartAttemptWithSelectedQuestionsAsync(int userId, int quizId, IEnumerable<int> questionIds);
        Task<QuizAttempt> SubmitAttemptAsync(int userId, SubmitQuizDto dto);
        Task<QuizReviewDto> GetAttemptReviewAsync(int attemptId, int userId);
    }
}
