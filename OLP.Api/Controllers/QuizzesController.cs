using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using OLP.Core.DTOs;
using OLP.Core.Entities;
using OLP.Core.Interfaces;

namespace OLP.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QuizzesController : ControllerBase
    {
        private readonly IQuizRepository _quizRepo;
        private readonly IQuestionRepository _questionRepo;
        private readonly IAnswerRepository _answerRepo;
        private readonly IQuizAttemptRepository _attemptRepo;
        private readonly IQuizService _quizService;

        public QuizzesController(
            IQuizRepository quizRepo,
            IQuestionRepository questionRepo,
            IAnswerRepository answerRepo,
            IQuizAttemptRepository attemptRepo,
            IQuizService quizService)
        {
            _quizRepo = quizRepo;
            _questionRepo = questionRepo;
            _answerRepo = answerRepo;
            _attemptRepo = attemptRepo;
            _quizService = quizService;
        }

        // ===============================
        // STUDENT ROUTES
        // ===============================

        // GET /api/quizzes/{id} → get quiz without correct answers
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetQuiz(int id)
        {
            var quiz = await _quizRepo.GetByIdWithQuestionsAsync(id);
            if (quiz == null)
                return NotFound();

            var result = new
            {
                quiz.Id,
                quiz.Title,
                quiz.PassingScore,
                Questions = quiz.Questions.Select(q => new
                {
                    q.Id,
                    q.QuestionText,
                    q.QuestionType,
                    Answers = q.Answers.Select(a => new
                    {
                        a.Id,
                        a.AnswerText
                    })
                })
            };

            return Ok(result);
        }

        // POST /api/quizzes/{id}/submit → student submits quiz
        [HttpPost("{id}/submit")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> SubmitQuiz(int id, [FromBody] SubmitQuizDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            dto.QuizId = id;

            var attempt = await _quizService.SubmitAttemptAsync(userId, dto);
            return Ok(attempt);
        }

        // GET /api/quizzes/{id}/attempts → get all attempts by current user for this quiz
        [HttpGet("{id}/attempts")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetUserAttempts(int id)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var attempts = await _attemptRepo.GetByUserIdAsync(userId);
            return Ok(attempts.Where(a => a.QuizId == id));
        }

        // GET /api/quizzes/attempts/{attemptId}/review → review attempt details
        [HttpGet("attempts/{attemptId}/review")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> ReviewAttempt(int attemptId)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var review = await _quizService.GetAttemptReviewAsync(attemptId, userId);
            return Ok(review);
        }

        // ===============================
        // ADMIN / SUPERADMIN ROUTES
        // ===============================

        // POST /api/quizzes → create a new quiz
        [HttpPost]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> CreateQuiz([FromBody] QuizCreateDto dto)
        {
            var quiz = new Quiz
            {
                CourseId = dto.CourseId,
                LessonId = dto.LessonId,
                Title = dto.Title,
                PassingScore = dto.PassingScore,
                TimeLimit = dto.TimeLimit,
                ShuffleQuestions = dto.ShuffleQuestions,
                AllowRetake = dto.AllowRetake
            };

            await _quizRepo.AddAsync(quiz);
            await _quizRepo.SaveChangesAsync();
            return Ok(quiz);
        }

        // POST /api/quizzes/{quizId}/questions → add a question (with answers)
        [HttpPost("{quizId}/questions")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> AddQuestion(int quizId, [FromBody] QuestionCreateDto dto)
        {
            var question = new Question
            {
                QuizId = quizId,
                QuestionText = dto.QuestionText,
                QuestionType = Enum.Parse<Core.Enums.QuestionType>(dto.QuestionType),
                Points = dto.Points
            };

            await _questionRepo.AddAsync(question);
            await _questionRepo.SaveChangesAsync();

            foreach (var ans in dto.Answers)
            {
                var answer = new Answer
                {
                    QuestionId = question.Id,
                    AnswerText = ans.AnswerText,
                    IsCorrect = ans.IsCorrect
                };
                await _answerRepo.AddAsync(answer);
            }

            await _answerRepo.SaveChangesAsync();
            return Ok(question);
        }

        // DELETE /api/questions/{id} → delete question
        [HttpDelete("/api/questions/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var question = await _questionRepo.GetByIdAsync(id);
            if (question == null)
                return NotFound();

            await _questionRepo.DeleteAsync(question);
            await _questionRepo.SaveChangesAsync();
            return Ok();
        }

        // DELETE /api/answers/{id} → delete answer
        [HttpDelete("/api/answers/{id}")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> DeleteAnswer(int id)
        {
            var answer = await _answerRepo.GetByIdAsync(id);
            if (answer == null)
                return NotFound();

            await _answerRepo.DeleteAsync(answer);
            await _answerRepo.SaveChangesAsync();
            return Ok();
        }
    }
}
