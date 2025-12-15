using System.Threading.Tasks;
using OLP.Core.Entities;
using OLP.Core.DTOs;

namespace OLP.Core.Interfaces

{
    public interface IQuizService
    {
        Task<QuizAttempt> SubmitAttemptAsync(int userId, SubmitQuizDto dto);
        Task<QuizReviewDto> GetAttemptReviewAsync(int attemptId, int userId);
    }
}
