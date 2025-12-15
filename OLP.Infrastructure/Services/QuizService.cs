#nullable disable
using Microsoft.EntityFrameworkCore;
using OLP.Core.DTOs;
using OLP.Core.Entities;
using OLP.Core.Interfaces;
using OLP.Infrastructure.Data;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace OLP.Infrastructure.Services
{
    public class QuizService : IQuizService
    {
        private readonly AppDbContext _context;
        private readonly IQuizAttemptRepository _attemptRepo;
        private readonly IQuizAttemptAnswerRepository _attemptAnswerRepo;

        public QuizService(
            AppDbContext context,
            IQuizAttemptRepository attemptRepo,
            IQuizAttemptAnswerRepository attemptAnswerRepo)
        {
            _context = context;
            _attemptRepo = attemptRepo;
            _attemptAnswerRepo = attemptAnswerRepo;
        }

        // ==================================================
        // When a student submits a quiz
        // ==================================================
        public async Task<QuizAttempt> SubmitAttemptAsync(int userId, SubmitQuizDto dto)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == dto.QuizId);

            if (quiz == null)
                throw new Exception("Quiz not found.");

            int totalScore = 0;
            int maxScore = quiz.Questions?.Sum(q => q.Points) ?? 0;

            var attempt = new QuizAttempt
            {
                QuizId = quiz.Id,
                UserId = userId,
                AttemptDate = DateTime.UtcNow
            };

            await _attemptRepo.AddAsync(attempt);
            await _attemptRepo.SaveChangesAsync(); // Get attempt.Id after save

            foreach (var ans in dto.Answers ?? Enumerable.Empty<SubmitAnswerDto>())
            {
                var question = quiz.Questions?.FirstOrDefault(q => q.Id == ans.QuestionId);
                if (question == null) continue;

                bool isCorrect = false;

                // Case 1: Short answer question
                if (question.QuestionType == Core.Enums.QuestionType.ShortAnswer)
                {
                    var correctAnswer = question.Answers?
                        .FirstOrDefault(a => a.IsCorrect)?.AnswerText;

                    isCorrect = string.Equals(
                        ans.GivenTextAnswer?.Trim(),
                        correctAnswer?.Trim(),
                        StringComparison.OrdinalIgnoreCase);
                }
                // Case 2: MCQ / True-False
                else if (ans.AnswerId != null)
                {
                    var selectedAnswer = question.Answers?
                        .FirstOrDefault(a => a.Id == ans.AnswerId);

                    if (selectedAnswer?.IsCorrect == true)
                        isCorrect = true;
                }

                if (isCorrect)
                    totalScore += question.Points;

                var attemptAnswer = new QuizAttemptAnswer
                {
                    QuizAttemptId = attempt.Id,
                    QuestionId = question.Id,
                    AnswerId = ans.AnswerId,
                    GivenTextAnswer = ans.GivenTextAnswer,
                    IsCorrect = isCorrect
                };

                await _attemptAnswerRepo.AddAsync(attemptAnswer);
            }

            await _attemptAnswerRepo.SaveChangesAsync();

            // 🧮 Calculate percentage and store as int
            if (maxScore > 0)
            {
                decimal percentage = ((decimal)totalScore / maxScore) * 100;
                attempt.Score = Convert.ToInt32(Math.Round(percentage));  // ✅ fix: explicit conversion
            }
            else
            {
                attempt.Score = 0;
            }

            await _attemptRepo.SaveChangesAsync();

            return attempt;
        }

        // ==================================================
        // Review a quiz attempt
        // ==================================================
        public async Task<QuizReviewDto> GetAttemptReviewAsync(int attemptId, int userId)
        {
            var attempt = await _attemptRepo.GetWithAnswersAsync(attemptId);

            if (attempt == null || attempt.UserId != userId)
                throw new Exception("Attempt not found or not accessible.");

            var review = new QuizReviewDto
            {
                QuizTitle = attempt.Quiz?.Title ?? "Unknown Quiz",
                Score = attempt.Score,
                AttemptDate = attempt.AttemptDate,
                Questions = attempt.Answers?
                    .Select(a => new QuizReviewQuestionDto
                    {
                        QuestionText = a.Question?.QuestionText ?? "Unknown question",
                        GivenTextAnswer = a.GivenTextAnswer,
                        CorrectAnswer = a.Question?.Answers?
                            .FirstOrDefault(ans => ans.IsCorrect)?.AnswerText,
                        IsCorrect = a.IsCorrect
                    })
                    .ToList() ?? new System.Collections.Generic.List<QuizReviewQuestionDto>()
            };

            return review;
        }
    }
}
